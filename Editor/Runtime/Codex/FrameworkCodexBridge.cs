using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    [InitializeOnLoad]
    internal static class FrameworkCodexBridge
    {
        private const string SchemaVersion = "1.0";
        private const string BridgeVersion = "1.0.0";
        private const double ScanIntervalSeconds = 0.2d;
        private const double StatusIntervalSeconds = 2d;
        private static readonly string[] PackageKinds = { "framework", "starter", "sdk", "template", "package" };
        private static readonly Regex OperationIdPattern = new Regex("^[a-f0-9]{32}$", RegexOptions.Compiled);
        private static readonly Regex PackageIdPattern = new Regex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);

        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static readonly string BridgeRoot = Path.Combine(ProjectRoot, "Library", "VidaFramework", "CodexBridge");
        private static readonly string RequestsPath = Path.Combine(BridgeRoot, "requests");
        private static readonly string ProcessingPath = Path.Combine(BridgeRoot, "processing");
        private static readonly string ResponsesPath = Path.Combine(BridgeRoot, "responses");
        private static readonly string StatusPath = Path.Combine(BridgeRoot, "status.json");

        private static bool _ready;
        private static bool _busy;
        private static string _activeOperationId = string.Empty;
        private static double _nextScanAt;
        private static double _nextStatusAt;

        static FrameworkCodexBridge()
        {
            try
            {
                Directory.CreateDirectory(RequestsPath);
                Directory.CreateDirectory(ProcessingPath);
                Directory.CreateDirectory(ResponsesPath);
                RecoverInterruptedOperations();
                CleanupOldResponses();
                _ready = true;
                WriteStatus();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("VIDA: Codex bridge could not initialize. " + exception.Message);
            }

            EditorApplication.update += Update;
            EditorApplication.quitting += Shutdown;
            AssemblyReloadEvents.beforeAssemblyReload += Shutdown;
            FrameworkSession.SessionChanged += HandleSessionChanged;
        }

        private static void Update()
        {
            if (!_ready)
            {
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            if (now >= _nextStatusAt)
            {
                _nextStatusAt = now + StatusIntervalSeconds;
                WriteStatus();
            }

            if (_busy || now < _nextScanAt)
            {
                return;
            }

            _nextScanAt = now + ScanIntervalSeconds;
            string requestPath = Directory.GetFiles(RequestsPath, "*.json", SearchOption.TopDirectoryOnly)
                .OrderBy(File.GetCreationTimeUtc)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(requestPath))
            {
                StartRequest(requestPath);
            }
        }

        private static void StartRequest(string requestPath)
        {
            string processingPath = Path.Combine(ProcessingPath, Path.GetFileName(requestPath));
            BridgeRequest request = null;
            try
            {
                File.Move(requestPath, processingPath);
                request = JsonUtility.FromJson<BridgeRequest>(File.ReadAllText(processingPath));
                ValidateRequest(request, processingPath);
                _busy = true;
                _activeOperationId = request.operationId;
                WriteStatus();
                _ = ProcessRequestAsync(request, processingPath);
            }
            catch (Exception exception)
            {
                string operationId = request?.operationId;
                if (!OperationIdPattern.IsMatch(operationId ?? string.Empty))
                {
                    operationId = Path.GetFileNameWithoutExtension(processingPath);
                }

                if (OperationIdPattern.IsMatch(operationId ?? string.Empty))
                {
                    WriteResponse(BridgeResponse.Failure(operationId, request?.command, "invalid_request", SafeMessage(exception)));
                }

                DeleteIfExists(processingPath);
                _busy = false;
                _activeOperationId = string.Empty;
                WriteStatus();
            }
        }

        private static async Task ProcessRequestAsync(BridgeRequest request, string processingPath)
        {
            BridgeResponse response;
            try
            {
                response = await ExecuteAsync(request);
            }
            catch (OperationCanceledException)
            {
                response = BridgeResponse.Failure(request.operationId, request.command, "operation_canceled", "The operation was canceled.");
            }
            catch (BridgeException exception)
            {
                response = BridgeResponse.Failure(request.operationId, request.command, exception.Code, exception.Message);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("VIDA: Codex bridge operation failed. " + exception.Message);
                response = BridgeResponse.Failure(request.operationId, request.command, "operation_failed", SafeMessage(exception));
            }

            try
            {
                WriteResponse(response);
            }
            finally
            {
                DeleteIfExists(processingPath);
                _busy = false;
                _activeOperationId = string.Empty;
                WriteStatus();
            }
        }

        private static async Task<BridgeResponse> ExecuteAsync(BridgeRequest request)
        {
            switch (request.command)
            {
                case "sign_in":
                {
                    bool alreadySignedIn = FrameworkSession.IsSignedIn;
                    if (!alreadySignedIn)
                    {
                        await FrameworkSession.SignInAsync(CancellationToken.None);
                    }

                    return BridgeResponse.SignInSuccess(request.operationId, alreadySignedIn);
                }
                case "list_packages":
                {
                    EnsureSignedIn();
                    List<StarterPackageInfo> packages = await LoadPackagesAsync(request.kind);
                    return BridgeResponse.PackageListSuccess(request.operationId, packages);
                }
                case "install_latest":
                {
                    EnsureSignedIn();
                    EnsureEditorIdle();
                    if (!PackageIdPattern.IsMatch(request.packageId ?? string.Empty))
                    {
                        throw new BridgeException("invalid_package_id", "The package ID is invalid.");
                    }

                    StarterPackageInfo package = await FindPackageAsync(request.packageId);
                    if (package == null)
                    {
                        throw new BridgeException("package_not_found", "The package is not visible in the current Vida catalog.");
                    }

                    if (string.Equals(package.Kind, "framework", StringComparison.Ordinal))
                    {
                        throw new BridgeException(
                            "framework_update_not_supported",
                            "Use Güncellemeleri kontrol et in Vida > Menu for the Framework package itself.");
                    }

                    if (!FrameworkUpdater.SupportsUnity(package.UnityMinVersion, Application.unityVersion))
                    {
                        throw new BridgeException(
                            "unity_version_not_supported",
                            "This package requires Unity " + package.UnityMinVersion + " or later.");
                    }

                    EnsureEditorIdle();
                    await FrameworkStoreClient.DownloadAndImportAsync(package, null, false);
                    return BridgeResponse.InstallSuccess(request.operationId, package);
                }
                default:
                    throw new BridgeException("command_not_supported", "The requested bridge command is not supported.");
            }
        }

        private static async Task<List<StarterPackageInfo>> LoadPackagesAsync(string kind)
        {
            if (!string.IsNullOrEmpty(kind) && Array.IndexOf(PackageKinds, kind) < 0)
            {
                throw new BridgeException("invalid_package_kind", "The package kind is invalid.");
            }

            if (!string.IsNullOrEmpty(kind))
            {
                return await FrameworkStoreClient.GetPackagesAsync(kind);
            }

            var packages = new List<StarterPackageInfo>();
            foreach (string packageKind in PackageKinds)
            {
                packages.AddRange(await FrameworkStoreClient.GetPackagesAsync(packageKind));
            }

            return packages.OrderBy(package => package.Kind, StringComparer.Ordinal)
                .ThenBy(package => package.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static async Task<StarterPackageInfo> FindPackageAsync(string packageId)
        {
            foreach (string kind in PackageKinds)
            {
                StarterPackageInfo package = (await FrameworkStoreClient.GetPackagesAsync(kind))
                    .SingleOrDefault(item => string.Equals(item.Id, packageId, StringComparison.Ordinal));
                if (package != null)
                {
                    return package;
                }
            }

            return null;
        }

        private static void EnsureSignedIn()
        {
            if (!FrameworkSession.IsSignedIn)
            {
                throw new BridgeException("sign_in_required", "Call vida_framework_sign_in before accessing Framework packages.");
            }
        }

        private static void EnsureEditorIdle()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new BridgeException("unity_busy", "Unity must be idle and outside Play Mode before importing a package.");
            }
        }

        private static void ValidateRequest(BridgeRequest request, string path)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (request == null
                || request.schemaVersion != SchemaVersion
                || !OperationIdPattern.IsMatch(request.operationId ?? string.Empty)
                || !string.Equals(Path.GetFileNameWithoutExtension(path), request.operationId, StringComparison.Ordinal)
                || request.createdAt < now - 60_000L
                || request.createdAt > now + 5_000L)
            {
                throw new InvalidDataException("The bridge request is invalid.");
            }
        }

        private static void RecoverInterruptedOperations()
        {
            foreach (string path in Directory.GetFiles(ProcessingPath, "*.json", SearchOption.TopDirectoryOnly))
            {
                string operationId = Path.GetFileNameWithoutExtension(path);
                if (OperationIdPattern.IsMatch(operationId))
                {
                    WriteResponse(BridgeResponse.Failure(
                        operationId,
                        string.Empty,
                        "editor_reloaded",
                        "Unity reloaded while the operation was running. Check the project state before retrying."));
                }

                DeleteIfExists(path);
            }
        }

        private static void CleanupOldResponses()
        {
            DateTime cutoff = DateTime.UtcNow.AddDays(-1d);
            foreach (string path in Directory.GetFiles(ResponsesPath, "*.json", SearchOption.TopDirectoryOnly))
            {
                if (File.GetLastWriteTimeUtc(path) < cutoff)
                {
                    DeleteIfExists(path);
                }
            }
        }

        private static void HandleSessionChanged()
        {
            WriteStatus();
        }

        private static void Shutdown()
        {
            if (!_ready)
            {
                return;
            }

            WriteStatus(false);
        }

        private static void WriteStatus(bool ready = true)
        {
            if (!_ready && ready)
            {
                return;
            }

            var status = new BridgeStatus
            {
                schemaVersion = SchemaVersion,
                bridgeVersion = BridgeVersion,
                ready = ready,
                projectPath = ProjectRoot,
                processId = System.Diagnostics.Process.GetCurrentProcess().Id,
                unityVersion = Application.unityVersion,
                frameworkVersion = ReadFrameworkVersion(),
                signedIn = FrameworkSession.IsSignedIn,
                busy = _busy,
                activeOperationId = _activeOperationId,
                updatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            WriteJsonAtomic(StatusPath, JsonUtility.ToJson(status));
        }

        private static void WriteResponse(BridgeResponse response)
        {
            string path = Path.Combine(ResponsesPath, response.operationId + ".json");
            WriteJsonAtomic(path, JsonUtility.ToJson(response));
        }

        private static void WriteJsonAtomic(string path, string json)
        {
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporary, json + Environment.NewLine);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(temporary, path);
        }

        private static string ReadFrameworkVersion()
        {
            try
            {
                UnityEditor.PackageManager.PackageInfo package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(FrameworkCodexBridge).Assembly);
                if (package != null && !string.IsNullOrWhiteSpace(package.version))
                {
                    return package.version;
                }

                string path = Path.Combine(Application.dataPath, "framework", "package.json");
                if (File.Exists(path))
                {
                    PackageManifest manifest = JsonUtility.FromJson<PackageManifest>(File.ReadAllText(path));
                    return manifest?.version ?? string.Empty;
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string SafeMessage(Exception exception)
        {
            return exception is InvalidDataException || exception is InvalidOperationException
                ? exception.Message
                : "The bridge operation failed.";
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        [Serializable]
        private sealed class BridgeRequest
        {
            public string schemaVersion;
            public string operationId;
            public string command;
            public string kind;
            public string packageId;
            public long createdAt;
        }

        [Serializable]
        private sealed class BridgeStatus
        {
            public string schemaVersion;
            public string bridgeVersion;
            public bool ready;
            public string projectPath;
            public int processId;
            public string unityVersion;
            public string frameworkVersion;
            public bool signedIn;
            public bool busy;
            public string activeOperationId;
            public long updatedAt;
        }

        [Serializable]
        private sealed class BridgeResponse
        {
            public string schemaVersion;
            public string operationId;
            public string command;
            public bool success;
            public string state;
            public bool signedIn;
            public bool alreadySignedIn;
            public string packageId;
            public string version;
            public PackageItem[] packages;
            public string errorCode;
            public string errorMessage;
            public long completedAt;

            internal static BridgeResponse Failure(string operationId, string command, string errorCode, string errorMessage)
            {
                return new BridgeResponse
                {
                    schemaVersion = SchemaVersion,
                    operationId = operationId,
                    command = command ?? string.Empty,
                    success = false,
                    state = "failed",
                    errorCode = errorCode,
                    errorMessage = errorMessage,
                    completedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
            }

            internal static BridgeResponse SignInSuccess(string operationId, bool alreadySignedIn)
            {
                return new BridgeResponse
                {
                    schemaVersion = SchemaVersion,
                    operationId = operationId,
                    command = "sign_in",
                    success = true,
                    state = "completed",
                    signedIn = true,
                    alreadySignedIn = alreadySignedIn,
                    completedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
            }

            internal static BridgeResponse PackageListSuccess(string operationId, IEnumerable<StarterPackageInfo> packages)
            {
                return new BridgeResponse
                {
                    schemaVersion = SchemaVersion,
                    operationId = operationId,
                    command = "list_packages",
                    success = true,
                    state = "completed",
                    signedIn = true,
                    packages = packages.Select(PackageItem.FromPackage).ToArray(),
                    completedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
            }

            internal static BridgeResponse InstallSuccess(string operationId, StarterPackageInfo package)
            {
                return new BridgeResponse
                {
                    schemaVersion = SchemaVersion,
                    operationId = operationId,
                    command = "install_latest",
                    success = true,
                    state = "completed",
                    signedIn = true,
                    packageId = package.Id,
                    version = package.Version,
                    completedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
            }
        }

        [Serializable]
        private sealed class PackageItem
        {
            public string id;
            public string kind;
            public string name;
            public string summary;
            public string version;
            public string versionLabel;
            public string unityMinVersion;

            internal static PackageItem FromPackage(StarterPackageInfo package)
            {
                return new PackageItem
                {
                    id = package.Id,
                    kind = package.Kind,
                    name = package.Name,
                    summary = package.Summary,
                    version = package.Version,
                    versionLabel = package.VersionLabel,
                    unityMinVersion = package.UnityMinVersion
                };
            }
        }

        [Serializable]
        private sealed class PackageManifest
        {
            public string version;
        }

        private sealed class BridgeException : Exception
        {
            internal BridgeException(string code, string message) : base(message)
            {
                Code = code;
            }

            internal string Code { get; }
        }
    }
}
