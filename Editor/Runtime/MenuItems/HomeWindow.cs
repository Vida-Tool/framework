using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public class HomeWindow
    {
        private const int MaxLoginAttempts = 10;
        private const int RetryDelayMs = 500;

        private readonly Action _repaint;
        private CancellationTokenSource _loginCancellation;
        private bool _isConnecting;
        private bool _isDisposed;
        private bool _hasLoginError;
        private string _loginStatus = "Enter your GitHub token to connect.";

        public HomeWindow(Action repaint)
        {
            _repaint = repaint;
        }

        /// <summary>
        /// Arayüzün çizimini gerçekleştirir.
        /// </summary>
        public void Draw(Vector2 contentSize)
        {
            if (!VidaFramework.Connection)
            {
                GitLogin(contentSize);
                return;
            }

            VidaPremiumGUI.DrawSectionHeader("Home", "Framework packages, templates and code helpers are ready.");
            VidaPremiumGUI.DrawCenteredState(
                "Connection Ready",
                "Starter, SDK and Templates tabs can now be managed from the left menu.",
                VidaPremiumGUI.GetPremiumTexture("status-connected.png"));
        }

        /// <summary>
        /// GitHub bağlantısı için giriş ekranını çizer.
        /// </summary>
        private void GitLogin(Vector2 contentSize)
        {
            const float pagePadding = 36f;
            float formWidth = Mathf.Max(320f, contentSize.x - pagePadding * 2f);

            VidaPremiumGUI.DrawContentBackground(new Rect(0f, 0f, contentSize.x, contentSize.y));
            GUILayout.Space(pagePadding);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(pagePadding);

                using (new GUILayout.VerticalScope(GUILayout.Width(formWidth)))
                {
                    VidaPremiumGUI.DrawSectionHeader("GitHub Connection", "Connect once to load Vida framework packages.");
                    using (new EditorGUI.DisabledScope(_isConnecting))
                    {
                        ApiKey = VidaPremiumGUI.DrawPasswordField(ApiKey, formWidth);
                    }

                    GUILayout.Space(8f);
                    VidaPremiumGUI.DrawInlineMessage(_loginStatus, _hasLoginError);
                    GUILayout.Space(10f);

                    using (new GUILayout.HorizontalScope())
                    {
                        string tryLabel = _isConnecting ? "Cancel" : "Try";
                        Texture2D tryIcon = VidaPremiumGUI.GetPremiumTexture(_isConnecting ? "icon-logout.png" : "icon-reload.png");
                        using (new EditorGUI.DisabledScope(!_isConnecting && string.IsNullOrWhiteSpace(ApiKey)))
                        {
                            if (VidaPremiumGUI.DrawHeaderAction(tryLabel, tryIcon, 160f, false, _isConnecting))
                            {
                                if (_isConnecting)
                                {
                                    CancelLogin();
                                }
                                else
                                {
                                    TryConnectOnceAsync(false);
                                }
                            }
                        }

                        GUILayout.Space(8f);

                        using (new EditorGUI.DisabledScope(_isConnecting || string.IsNullOrWhiteSpace(ApiKey)))
                        {
                            if (VidaPremiumGUI.DrawHeaderAction("Login", VidaPremiumGUI.GetPremiumTexture("icon-login.png"), 160f, true))
                            {
                                StartLogin();
                            }
                        }
                    }
                }

                GUILayout.FlexibleSpace();
            }

            GUILayout.FlexibleSpace();
        }

        /// <summary>
        /// Tries the saved token once.
        /// </summary>
        public void StartAutoConnect()
        {
            if (!VidaFramework.AutoConnect || VidaFramework.Connection || _isConnecting || string.IsNullOrWhiteSpace(ApiKey))
            {
                return;
            }

            TryConnectOnceAsync(true);
        }

        /// <summary>
        /// Starts login attempts in the main window.
        /// </summary>
        private void StartLogin()
        {
            if (VidaFramework.Connection || _isConnecting)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                SetLoginStatus("Enter your GitHub token first.", true);
                return;
            }

            LoginAsync();
        }

        public void Dispose()
        {
            _isDisposed = true;
            _loginCancellation?.Cancel();
            _loginCancellation?.Dispose();
        }

        private async void TryConnectOnceAsync(bool isAutoConnect)
        {
            if (!BeginLogin())
            {
                return;
            }

            CancellationToken cancellationToken = _loginCancellation.Token;
            SetLoginStatus(isAutoConnect ? "Checking saved token..." : "Trying connection...", false);

            try
            {
                bool result = await GithubConnector.TryConnectAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    SetLoginStatus("Connection canceled.", false);
                    return;
                }

                if (result)
                {
                    CompleteLogin();
                    return;
                }

                string failedStatus = isAutoConnect
                    ? "Saved token could not connect. Try again when ready."
                    : "Connection failed. Check the token and try again.";
                SetLoginStatus(failedStatus, true);
            }
            catch (OperationCanceledException)
            {
                SetLoginStatus("Connection canceled.", false);
            }
            catch (Exception)
            {
                SetLoginStatus("Connection error. Check the token and network.", true);
            }
            finally
            {
                EndLogin();
            }
        }

        private async void LoginAsync()
        {
            if (!BeginLogin())
            {
                return;
            }

            CancellationToken cancellationToken = _loginCancellation.Token;

            try
            {
                for (int attempt = 1; attempt <= MaxLoginAttempts; attempt++)
                {
                    SetLoginStatus($"Attempt {attempt} of {MaxLoginAttempts}...", false);

                    bool result = await GithubConnector.TryConnectAsync(cancellationToken);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        SetLoginStatus("Connection canceled.", false);
                        return;
                    }

                    if (result)
                    {
                        CompleteLogin();
                        return;
                    }

                    if (attempt < MaxLoginAttempts)
                    {
                        SetLoginStatus($"Attempt {attempt} failed. Retrying...", true);
                        await Task.Delay(RetryDelayMs, cancellationToken);
                    }
                }

                SetLoginStatus($"Connection failed after {MaxLoginAttempts} attempts. Check the token.", true);
            }
            catch (OperationCanceledException)
            {
                SetLoginStatus("Connection canceled.", false);
            }
            catch (Exception)
            {
                SetLoginStatus("Connection error. Check the token and network.", true);
            }
            finally
            {
                EndLogin();
            }
        }

        private bool BeginLogin()
        {
            if (_isDisposed || _isConnecting || string.IsNullOrWhiteSpace(ApiKey))
            {
                return false;
            }

            _loginCancellation?.Dispose();
            _loginCancellation = new CancellationTokenSource();
            _isConnecting = true;
            _hasLoginError = false;
            Repaint();
            return true;
        }

        private void EndLogin()
        {
            _isConnecting = false;
            Repaint();
        }

        private void CancelLogin(bool updateStatus = true)
        {
            if (!_isConnecting)
            {
                return;
            }

            VidaFramework.AutoConnect = false;
            _loginCancellation?.Cancel();
            if (updateStatus)
            {
                SetLoginStatus("Canceling connection...", false);
            }
        }

        private void CompleteLogin()
        {
            VidaFramework.Connection = true;
            VidaFramework.AutoConnect = true;
            SetLoginStatus("Connected.", false);
        }

        private void SetLoginStatus(string status, bool hasError)
        {
            _loginStatus = status;
            _hasLoginError = hasError;
            Repaint();
        }

        private void Repaint()
        {
            if (!_isDisposed)
            {
                _repaint?.Invoke();
            }
        }

        private string ApiKey
        {
            get => GithubConnector.ApiKey;
            set => GithubConnector.ApiKey = value;
        }
    }
}
