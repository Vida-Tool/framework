using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Vida.Framework.Editor
{
    internal static class FrameworkSession
    {
        private const string IdentityBaseUrl = "https://auth.vida.games";
        private const string ClientId = "vida-framework-editor";
        private const string CallbackPath = "/vida/auth/callback";
        private const string SessionHandleKey = "VidaFramework.Session.Handle";
        private const string SessionEmailKey = "VidaFramework.Session.Email";
        private const string SessionRoleKey = "VidaFramework.Session.Role";
        private const string SessionStudioKey = "VidaFramework.Session.Studio";
        private const string SessionExpiryKey = "VidaFramework.Session.Expiry";

        internal static long Generation { get; private set; }
        internal static event Action SessionChanged;

        public static bool IsSignedIn
        {
            get
            {
                if (string.IsNullOrEmpty(SessionHandle))
                {
                    return false;
                }

                if (ExpiresAt > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    return true;
                }

                Clear();
                return false;
            }
        }

        public static string Email => SessionState.GetString(SessionEmailKey, string.Empty);
        public static string Role => SessionState.GetString(SessionRoleKey, string.Empty);
        public static string StudioId => SessionState.GetString(SessionStudioKey, string.Empty);
        internal static string SessionHandle => SessionState.GetString(SessionHandleKey, string.Empty);
        internal static string Authorization => string.IsNullOrEmpty(SessionHandle) ? string.Empty : "Bearer " + SessionHandle;

        internal static SessionSnapshot Capture()
        {
            if (!IsSignedIn)
            {
                throw new InvalidOperationException("Sign in with Vida to access Framework packages.");
            }

            return new SessionSnapshot(Generation, StudioId, Authorization);
        }

        internal static bool IsCurrent(SessionSnapshot snapshot)
        {
            return snapshot.Generation == Generation
                   && string.Equals(snapshot.StudioId, StudioId, StringComparison.Ordinal);
        }

        internal static void EnsureCurrent(SessionSnapshot snapshot)
        {
            if (!IsCurrent(snapshot))
            {
                throw new OperationCanceledException("The Vida studio session changed while the request was running.");
            }
        }

        internal static bool ClearIfCurrent(SessionSnapshot snapshot)
        {
            if (!IsCurrent(snapshot))
            {
                return false;
            }

            Clear();
            return true;
        }

        private static long ExpiresAt
        {
            get
            {
                string value = SessionState.GetString(SessionExpiryKey, "0");
                return long.TryParse(value, out long expiresAt) ? expiresAt : 0L;
            }
        }

        public static async Task SignInAsync(CancellationToken cancellationToken)
        {
            string state = CreateRandomValue(32);
            string verifier = CreateRandomValue(64);
            string challenge;
            using (SHA256 sha256 = SHA256.Create())
            {
                challenge = Base64Url(sha256.ComputeHash(Encoding.ASCII.GetBytes(verifier)));
            }

            using HttpListener listener = CreateLoopbackListener(out string redirectUri);
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMinutes(10));
            string authorizeUrl = IdentityBaseUrl + "/sso/authorize"
                                  + "?response_type=code"
                                  + "&client_id=" + UnityWebRequest.EscapeURL(ClientId)
                                  + "&redirect_uri=" + UnityWebRequest.EscapeURL(redirectUri)
                                  + "&state=" + UnityWebRequest.EscapeURL(state)
                                  + "&code_challenge=" + UnityWebRequest.EscapeURL(challenge)
                                  + "&code_challenge_method=S256";

            Application.OpenURL(authorizeUrl);
            string code = await ReceiveAuthorizationCodeAsync(listener, new Uri(redirectUri), state, timeout.Token);
            SessionResponse response = await ExchangeCodeAsync(code, redirectUri, verifier, timeout.Token);

            if (!IsValidSessionResponse(response))
            {
                throw new InvalidOperationException("Vida Identity returned an invalid Framework session.");
            }

            Replace(response);
        }

        public static async Task SignOutAsync(CancellationToken cancellationToken = default)
        {
            string handle = SessionHandle;
            Clear();
            if (string.IsNullOrEmpty(handle))
            {
                return;
            }

            RevokeRequest body = new RevokeRequest
            {
                client_id = ClientId,
                session_handle = handle
            };

            try
            {
                await SendJsonAsync(IdentityBaseUrl + "/sso/revoke", JsonUtility.ToJson(body), cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Debug.LogWarning("VIDA: The local Framework session ended, but server revocation could not be confirmed.");
            }
        }

        public static void Clear()
        {
            SessionState.EraseString(SessionHandleKey);
            SessionState.EraseString(SessionEmailKey);
            SessionState.EraseString(SessionRoleKey);
            SessionState.EraseString(SessionStudioKey);
            SessionState.EraseString(SessionExpiryKey);
            AdvanceGeneration();
        }

        private static void Replace(SessionResponse response)
        {
            SessionState.SetString(SessionHandleKey, response.sessionHandle);
            SessionState.SetString(SessionEmailKey, response.email ?? string.Empty);
            SessionState.SetString(SessionRoleKey, response.role ?? string.Empty);
            SessionState.SetString(SessionStudioKey, response.studioId ?? string.Empty);
            SessionState.SetString(SessionExpiryKey, response.expiresAt.ToString());
            AdvanceGeneration();
        }

        private static void AdvanceGeneration()
        {
            Generation++;
            SessionChanged?.Invoke();
        }

        internal readonly struct SessionSnapshot
        {
            internal SessionSnapshot(long generation, string studioId, string authorization)
            {
                Generation = generation;
                StudioId = studioId;
                Authorization = authorization;
            }

            internal long Generation { get; }
            internal string StudioId { get; }
            internal string Authorization { get; }
        }

        private static HttpListener CreateLoopbackListener(out string redirectUri)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                byte[] portBytes = new byte[2];
                using (RandomNumberGenerator random = RandomNumberGenerator.Create())
                {
                    random.GetBytes(portBytes);
                }

                int port = 49152 + BitConverter.ToUInt16(portBytes, 0) % 16384;
                HttpListener listener = new HttpListener();
                listener.Prefixes.Add($"http://127.0.0.1:{port}/");

                try
                {
                    listener.Start();
                    redirectUri = $"http://127.0.0.1:{port}{CallbackPath}";
                    return listener;
                }
                catch (HttpListenerException)
                {
                    listener.Close();
                }
            }

            throw new InvalidOperationException("A local callback port could not be opened for Vida sign-in.");
        }

        private static async Task<string> ReceiveAuthorizationCodeAsync(
            HttpListener listener,
            Uri expectedRedirect,
            string expectedState,
            CancellationToken cancellationToken)
        {
            using CancellationTokenRegistration registration = cancellationToken.Register(listener.Close);
            HttpListenerContext context;

            try
            {
                context = await listener.GetContextAsync();
            }
            catch (Exception exception) when (cancellationToken.IsCancellationRequested
                                              && (exception is HttpListenerException || exception is ObjectDisposedException))
            {
                throw new OperationCanceledException(cancellationToken);
            }

            bool validCallback = string.Equals(context.Request.HttpMethod, "GET", StringComparison.Ordinal)
                                 && string.Equals(context.Request.Url?.Host, "127.0.0.1", StringComparison.Ordinal)
                                 && context.Request.Url?.Port == expectedRedirect.Port
                                 && string.Equals(context.Request.Url?.AbsolutePath, CallbackPath, StringComparison.Ordinal)
                                 && context.Request.QueryString.Count == 2;
            string[] states = context.Request.QueryString.GetValues("state");
            string[] codes = context.Request.QueryString.GetValues("code");
            bool validState = states is { Length: 1 }
                              && CryptographicOperations.FixedTimeEquals(
                                  Encoding.UTF8.GetBytes(states[0]),
                                  Encoding.UTF8.GetBytes(expectedState));
            bool validCode = codes is { Length: 1 } && IsLowerHex(codes[0], 64);

            if (!validCallback || !validState || !validCode)
            {
                await WriteBrowserResponseAsync(context.Response, 400, "Vida sign-in could not be completed. Return to Unity and try again.");
                throw new InvalidOperationException("Vida Identity callback validation failed.");
            }

            await WriteBrowserResponseAsync(context.Response, 200, "Vida sign-in completed. You can return to Unity.");
            return codes[0];
        }

        private static async Task WriteBrowserResponseAsync(HttpListenerResponse response, int statusCode, string message)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(
                "<!doctype html><html><head><meta charset=\"utf-8\"><title>Vida</title></head>"
                + "<body style=\"font-family:system-ui;background:#111;color:#eee;padding:40px\">"
                + "<h1>Vida Framework</h1><p>" + message + "</p></body></html>");
            response.StatusCode = statusCode;
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            response.Close();
        }

        private static async Task<SessionResponse> ExchangeCodeAsync(
            string code,
            string redirectUri,
            string verifier,
            CancellationToken cancellationToken)
        {
            ExchangeRequest body = new ExchangeRequest
            {
                grant_type = "authorization_code",
                client_id = ClientId,
                code = code,
                redirect_uri = redirectUri,
                code_verifier = verifier
            };
            string json = await SendJsonAsync(IdentityBaseUrl + "/sso/exchange", JsonUtility.ToJson(body), cancellationToken);
            return JsonUtility.FromJson<SessionResponse>(json);
        }

        private static async Task<string> SendJsonAsync(string url, string json, CancellationToken cancellationToken)
        {
            using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.timeout = 30;
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            try
            {
                while (!operation.isDone)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Yield();
                }
            }
            catch (OperationCanceledException)
            {
                request.Abort();
                throw;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                ErrorResponse error = null;
                if (!string.IsNullOrWhiteSpace(request.downloadHandler.text))
                {
                    error = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
                }

                string code = string.IsNullOrEmpty(error?.error) ? request.responseCode.ToString() : error.error;
                throw new InvalidOperationException("Vida Identity request failed: " + code);
            }

            return request.downloadHandler.text;
        }

        private static string CreateRandomValue(int byteCount)
        {
            byte[] bytes = new byte[byteCount];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create())
            {
                random.GetBytes(bytes);
            }
            return Base64Url(bytes);
        }

        private static bool IsLowerHex(string value, int length)
        {
            if (value == null || value.Length != length)
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (!char.IsDigit(character) && (character < 'a' || character > 'f'))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidSessionResponse(SessionResponse response)
        {
            return response != null
                   && response.issuer == IdentityBaseUrl
                   && !string.IsNullOrWhiteSpace(response.subject)
                   && IsLowerHex(response.sessionHandle, 64)
                   && !string.IsNullOrWhiteSpace(response.studioId)
                   && response.productId == "packages"
                   && (response.role == "member" || response.role == "admin")
                   && response.permissions != null
                   && Array.IndexOf(response.permissions, "access") >= 0
                   && response.expiresAt > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private static string Base64Url(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        [Serializable]
        private sealed class ExchangeRequest
        {
            public string grant_type;
            public string client_id;
            public string code;
            public string redirect_uri;
            public string code_verifier;
        }

        [Serializable]
        private sealed class RevokeRequest
        {
            public string client_id;
            public string session_handle;
        }

        [Serializable]
        private sealed class ErrorResponse
        {
            public string error;
        }

        [Serializable]
        private sealed class SessionResponse
        {
            public string issuer;
            public string subject;
            public string email;
            public string studioId;
            public string productId;
            public string role;
            public string[] permissions;
            public long expiresAt;
            public string sessionHandle;
        }
    }
}
