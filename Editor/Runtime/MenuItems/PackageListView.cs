using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    internal sealed class PackageListView
    {
        private StarterPackageInfo _expandedPackage;
        private PackageDetails _details;
        private string _detailsError;
        private bool _isLoadingDetails;
        private bool _isDownloading;
        private int _detailsRequestVersion;

        public void DrawPackage(StarterPackageInfo package, float windowWidth, bool isDisabled)
        {
            bool isExpanded = IsExpanded(package);
            PackageRowAction action = VidaPremiumGUI.DrawPackageRow(
                package.GetDisplayInfo(),
                windowWidth,
                isDisabled || _isDownloading,
                isExpanded);

            if (action == PackageRowAction.ToggleDetails)
            {
                ToggleDetails(package);
                isExpanded = IsExpanded(package);
            }
            else if (action == PackageRowAction.DownloadLatest)
            {
                _ = DownloadAsync(package, null);
            }

            if (isExpanded)
            {
                GUILayout.Space(4f);
                DrawExpandedDetails(package);
            }
        }

        public void Reset()
        {
            _detailsRequestVersion++;
            _expandedPackage = null;
            _details = null;
            _detailsError = null;
            _isLoadingDetails = false;
        }

        private bool IsExpanded(StarterPackageInfo package)
        {
            return package != null
                   && _expandedPackage != null
                   && string.Equals(_expandedPackage.Id, package.Id, StringComparison.Ordinal);
        }

        private void ToggleDetails(StarterPackageInfo package)
        {
            _detailsRequestVersion++;
            if (IsExpanded(package))
            {
                _expandedPackage = null;
                _details = null;
                _detailsError = null;
                _isLoadingDetails = false;
                return;
            }

            _expandedPackage = package;
            _details = null;
            _detailsError = null;
            _isLoadingDetails = true;
            _ = LoadDetailsAsync(package, _detailsRequestVersion);
        }

        private void DrawExpandedDetails(StarterPackageInfo package)
        {
            Rect panelRect = EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            VidaPremiumGUI.DrawFrame(panelRect, "frame-panel.png");
            GUILayout.Space(14f);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(16f);
                using (new GUILayout.VerticalScope())
                {
                    if (_isLoadingDetails)
                    {
                        DrawCatalogSummary(package);
                        GUILayout.Space(10f);
                        VidaPremiumGUI.DrawInlineMessage("Detailed release information is loading…", false);
                    }
                    else if (!string.IsNullOrEmpty(_detailsError))
                    {
                        DrawCatalogSummary(package);
                        GUILayout.Space(10f);
                        VidaPremiumGUI.DrawInlineMessage("Package details could not be loaded: " + _detailsError, true);
                        GUILayout.Space(8f);
                        if (VidaPremiumGUI.DrawHeaderAction("Retry", VidaPremiumGUI.GetPremiumTexture("icon-reload.png"), 104f))
                        {
                            _detailsRequestVersion++;
                            _detailsError = null;
                            _isLoadingDetails = true;
                            _ = LoadDetailsAsync(package, _detailsRequestVersion);
                        }
                    }
                    else if (_details != null)
                    {
                        DrawDetails(package, _details);
                    }
                }
                GUILayout.Space(16f);
            }
            GUILayout.Space(14f);
            EditorGUILayout.EndVertical();
        }

        private static void DrawCatalogSummary(StarterPackageInfo package)
        {
            if (!string.IsNullOrWhiteSpace(package.Summary))
            {
                VidaPremiumGUI.DrawBodyText(package.Summary);
                GUILayout.Space(8f);
            }

            string minimumUnity = string.IsNullOrWhiteSpace(package.UnityMinVersion)
                ? "Not specified"
                : package.UnityMinVersion;
            VidaPremiumGUI.DrawBodyText("Kind: " + package.Kind + "    Minimum Unity: " + minimumUnity, true);
        }

        private void DrawDetails(StarterPackageInfo package, PackageDetails details)
        {
            if (!string.IsNullOrWhiteSpace(details.Description))
            {
                VidaPremiumGUI.DrawBodyText(details.Description);
                GUILayout.Space(10f);
            }

            string tags = details.Tags.Length == 0 ? "—" : string.Join(", ", details.Tags);
            VidaPremiumGUI.DrawBodyText("Kind: " + details.Kind + "    Tags: " + tags, true);
            string minimumUnity = string.IsNullOrWhiteSpace(details.UnityMinVersion)
                ? "Not specified"
                : details.UnityMinVersion;
            VidaPremiumGUI.DrawBodyText("Minimum Unity: " + minimumUnity, true);
            GUILayout.Space(18f);
            VidaPremiumGUI.DrawBodyText("Published versions");
            GUILayout.Space(8f);

            foreach (PackageVersionInfo version in details.Versions)
            {
                Rect versionRect = EditorGUILayout.BeginVertical();
                VidaPremiumGUI.DrawFrame(versionRect, "frame-row.png");
                GUILayout.Space(10f);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(12f);
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
                            _ = DownloadAsync(package, version);
                        }
                    }
                    GUILayout.Space(12f);
                }

                if (!string.IsNullOrWhiteSpace(version.ReleaseNotes))
                {
                    GUILayout.Space(8f);
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Space(12f);
                        VidaPremiumGUI.DrawBodyText(version.ReleaseNotes, true);
                        GUILayout.Space(12f);
                    }
                }

                GUILayout.Space(12f);
                EditorGUILayout.EndVertical();
                GUILayout.Space(6f);
            }
        }

        private async Task LoadDetailsAsync(StarterPackageInfo package, int requestVersion)
        {
            try
            {
                PackageDetails details = await FrameworkStoreClient.GetPackageDetailsAsync(package);
                if (requestVersion == _detailsRequestVersion && IsExpanded(package))
                {
                    _details = details;
                }
            }
            catch (OperationCanceledException)
            {
                if (requestVersion == _detailsRequestVersion && IsExpanded(package))
                {
                    Reset();
                }
            }
            catch (Exception exception)
            {
                if (requestVersion == _detailsRequestVersion && IsExpanded(package))
                {
                    _detailsError = exception.Message;
                    Debug.LogError("Framework package details could not be loaded: " + exception.Message);
                }
            }
            finally
            {
                if (requestVersion == _detailsRequestVersion && IsExpanded(package))
                {
                    _isLoadingDetails = false;
                }
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        private async Task DownloadAsync(StarterPackageInfo package, PackageVersionInfo version)
        {
            if (_isDownloading)
            {
                return;
            }

            _isDownloading = true;
            DownloadProgressWindow.Controller progress = null;
            EditorApplication.QueuePlayerLoopUpdate();
            try
            {
                string displayVersion = version == null ? package.DisplayVersion : version.DisplayVersion;
                progress = DownloadProgressWindow.Show("Download", package.Name + " " + displayVersion + " indiriliyor...");
                progress.SetIndeterminate();
                if (version == null)
                {
                    await FrameworkStoreClient.DownloadAndImportAsync(package, progress);
                }
                else
                {
                    await FrameworkStoreClient.DownloadAndImportAsync(package, version, progress);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError("Framework package could not be downloaded: " + exception.Message);
                EditorUtility.DisplayDialog("Download failed", exception.Message, "OK");
            }
            finally
            {
                progress?.Close();
                _isDownloading = false;
                EditorApplication.QueuePlayerLoopUpdate();
            }
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
