using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public sealed class PackageDetailsWindow : EditorWindow
    {
        private static readonly System.Collections.Generic.HashSet<PackageDetailsWindow> OpenWindows =
            new System.Collections.Generic.HashSet<PackageDetailsWindow>();

        private StarterPackageInfo _package;
        private PackageDetails _details;
        private string _error;
        private bool _isLoading;
        private bool _isDownloading;
        private Vector2 _scroll;
        private long _sessionGeneration;
        private string _studioId;

        public static void Open(StarterPackageInfo package)
        {
            FrameworkSession.SessionSnapshot session = FrameworkSession.Capture();
            PackageDetailsWindow window = CreateInstance<PackageDetailsWindow>();
            window._package = package;
            window._sessionGeneration = session.Generation;
            window._studioId = session.StudioId;
            window.titleContent = new GUIContent(package.Name);
            window.minSize = new Vector2(560f, 420f);
            window.ShowUtility();
            _ = window.LoadAsync();
        }

        internal static void CloseForSessionChange()
        {
            PackageDetailsWindow[] windows = new PackageDetailsWindow[OpenWindows.Count];
            OpenWindows.CopyTo(windows);
            foreach (PackageDetailsWindow window in windows)
            {
                window?.InvalidateAndClose();
            }
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            OpenWindows.Add(this);
            FrameworkSession.SessionChanged += InvalidateAndClose;
        }

        private void OnDisable()
        {
            FrameworkSession.SessionChanged -= InvalidateAndClose;
            OpenWindows.Remove(this);
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.MouseMove) Repaint();
            if (!IsCurrentSession())
            {
                InvalidateAndClose();
                return;
            }

            VidaPremiumGUI.DrawWindowBackground(new Rect(0f, 0f, position.width, position.height));
            GUILayout.Space(18f);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(20f);
                using (new GUILayout.VerticalScope(GUILayout.Width(position.width - 40f)))
                {
                    VidaPremiumGUI.DrawHeaderInfo(_package?.Name ?? "Package", _package?.Summary ?? string.Empty);
                    GUILayout.Space(12f);

                    if (_isLoading)
                    {
                        VidaPremiumGUI.DrawCenteredState(
                            "Paket ayrıntıları yükleniyor...",
                            "Yayınlanmış sürümler hazırlanıyor.",
                            VidaPremiumGUI.GetPremiumTexture("status-refreshing.png"));
                    }
                    else if (!string.IsNullOrEmpty(_error))
                    {
                        if (VidaPremiumGUI.DrawRetryState("Paket ayrıntıları alınamadı.", _error))
                        {
                            _ = LoadAsync();
                        }
                    }
                    else if (_details != null)
                    {
                        DrawDetails();
                    }
                }

                GUILayout.Space(20f);
            }
        }

        private void DrawDetails()
        {
            _scroll = GUILayout.BeginScrollView(_scroll);
            if (!string.IsNullOrWhiteSpace(_details.Description))
            {
                VidaPremiumGUI.DrawBodyText(_details.Description);
                GUILayout.Space(12f);
            }

            string tags = _details.Tags.Length == 0 ? "—" : string.Join(", ", _details.Tags);
            VidaPremiumGUI.DrawBodyText("Kind: " + _details.Kind + "    Tags: " + tags, true);
            VidaPremiumGUI.DrawBodyText("Minimum Unity: " + (_details.UnityMinVersion ?? "Not specified"), true);
            GUILayout.Space(24f);
            VidaPremiumGUI.DrawBodyText("Published versions");
            GUILayout.Space(12f);
            foreach (PackageVersionInfo version in _details.Versions)
            {
                Rect row = EditorGUILayout.BeginVertical();
                VidaPremiumGUI.DrawFrame(row, "frame-row.png");
                GUILayout.Space(12f);
                using (new GUILayout.HorizontalScope())
                {
                    using (new GUILayout.VerticalScope())
                    {
                        VidaPremiumGUI.DrawBodyText(version.DisplayVersion);
                        if (string.IsNullOrWhiteSpace(version.VersionLabel))
                        {
                            VidaPremiumGUI.DrawBodyText(version.Filename, true);
                        }
                        VidaPremiumGUI.DrawBodyText(FormatSize(version.Size) + "  ·  " + FormatDate(version.PublishedAt), true);
                    }
                    GUILayout.Space(12f);
                    using (new EditorGUI.DisabledScope(_isDownloading))
                    {
                        if (VidaPremiumGUI.DrawHeaderAction("Download", VidaPremiumGUI.GetPremiumTexture("icon-download.png"), 112f, true))
                        {
                            _ = DownloadAsync(version);
                        }
                    }
                }
                if (!string.IsNullOrWhiteSpace(version.ReleaseNotes))
                {
                    GUILayout.Space(10f);
                    VidaPremiumGUI.DrawBodyText(version.ReleaseNotes, true);
                }
                GUILayout.Space(16f);
                EditorGUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
        }

        protected override void OnBackingScaleFactorChanged()
        {
            VidaPremiumGUI.ResetStyles();
            Repaint();
        }

        private async Task LoadAsync()
        {
            if (_isLoading || _package == null)
            {
                return;
            }

            _isLoading = true;
            _error = null;
            Repaint();
            try
            {
                PackageDetails details = await FrameworkStoreClient.GetPackageDetailsAsync(_package);
                if (!IsCurrentSession())
                {
                    return;
                }

                _details = details;
            }
            catch (OperationCanceledException) when (!IsCurrentSession())
            {
                InvalidateAndClose();
            }
            catch (Exception exception)
            {
                _error = exception.Message;
                Debug.LogError("Framework paket ayrıntıları alınamadı: " + exception.Message);
            }
            finally
            {
                _isLoading = false;
                if (_package != null)
                {
                    Repaint();
                }
            }
        }

        private async Task DownloadAsync(PackageVersionInfo version)
        {
            if (_isDownloading)
            {
                return;
            }

            _isDownloading = true;
            DownloadProgressWindow.Controller progress = null;
            Repaint();
            try
            {
                progress = DownloadProgressWindow.Show("Download", _package.Name + " " + version.DisplayVersion + " indiriliyor...");
                await FrameworkStoreClient.DownloadAndImportAsync(_package, version, progress);
            }
            catch (OperationCanceledException) when (!IsCurrentSession())
            {
                InvalidateAndClose();
            }
            catch (Exception exception)
            {
                Debug.LogError("Framework paketi indirilemedi: " + exception.Message);
                EditorUtility.DisplayDialog("Download failed", exception.Message, "OK");
            }
            finally
            {
                progress?.Close();
                _isDownloading = false;
                if (_package != null)
                {
                    Repaint();
                }
            }
        }

        private bool IsCurrentSession()
        {
            return _package != null
                   && FrameworkSession.IsSignedIn
                   && _sessionGeneration == FrameworkSession.Generation
                   && string.Equals(_studioId, FrameworkSession.StudioId, StringComparison.Ordinal);
        }

        private void InvalidateAndClose()
        {
            _package = null;
            _details = null;
            _error = null;
            _isLoading = false;
            _isDownloading = false;
            Close();
        }

        private static string FormatSize(long bytes)
        {
            return bytes >= 1024L * 1024L
                ? (bytes / (1024f * 1024f)).ToString("0.0") + " MB"
                : (bytes / 1024f).ToString("0.0") + " KB";
        }

        private static string FormatDate(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).ToLocalTime().ToString("yyyy-MM-dd");
        }
    }
}
