using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public class SdkWindow
    {
        private const string PackageDirectory = "Sdk";

        private bool _initialized;
        private bool _isLoading;
        private bool _isRefreshing;
        private string _errorMessage;
        private Vector2 _scroll;
        private List<StarterPackageInfo> _packages;
        private static bool _resetRequested;
        private static bool _reloadRequested;

        public void Draw(Vector2 windowSize)
        {
            if (_resetRequested)
            {
                ClearWindowData();
                _resetRequested = false;
            }

            if (_reloadRequested)
            {
                ClearWindowData();
                _reloadRequested = false;
                _initialized = true;
                _ = LoadPackagesAsync(true);
            }

            if (!_initialized && !_isLoading)
            {
                _initialized = true;
                _ = LoadPackagesAsync(false);
            }

            GUILayout.BeginVertical();
            VidaPremiumGUI.DrawSectionHeader("SDK Packages", "SDK integrations and optional packages for the current project.");
            VidaPremiumGUI.DrawPackageTableHeader(windowSize.x);
            GUILayout.Space(6f);

            if (_isLoading)
            {
                VidaPremiumGUI.DrawCenteredState(
                    "SDK paketleri yükleniyor...",
                    "Cache varsa önce hızlı liste gelir, ardından arka planda güncellenir.",
                    VidaPremiumGUI.GetPremiumTexture("status-refreshing.png"));
            }
            else if (!string.IsNullOrEmpty(_errorMessage))
            {
                if (VidaPremiumGUI.DrawRetryState("SDK paketleri alınamadı.", _errorMessage))
                {
                    _ = LoadPackagesAsync(true);
                }
            }
            else if (_packages is { Count: > 0 })
            {
                _scroll = GUILayout.BeginScrollView(_scroll);
                foreach (StarterPackageInfo package in _packages)
                {
                    PackageDisplayInfo displayInfo = package.GetDisplayInfo();
                    if (VidaPremiumGUI.DrawPackageRow(displayInfo, windowSize.x, _isLoading))
                    {
                        _ = DownloadPackageAsync(package);
                    }
                    GUILayout.Space(6f);
                }
                GUILayout.EndScrollView();
            }
            else
            {
                if (VidaPremiumGUI.DrawRetryState("Gösterilecek SDK paketi bulunamadı.", "Repository listesini tekrar kontrol edebilirsin."))
                {
                    _ = LoadPackagesAsync(true);
                }
            }

            GUILayout.EndVertical();
        }

        private async Task LoadPackagesAsync(bool forceRefresh)
        {
            if (_isLoading)
            {
                return;
            }

            bool shouldRefreshAfterLoad = !forceRefresh && GithubConnector.HasPersistentUnityPackageCache(PackageDirectory);
            bool refreshAfterLoad = false;
            _isLoading = true;
            _errorMessage = null;
            try
            {
                _packages = await GithubConnector.GetSdkPackagesAsync(forceRefresh);
                refreshAfterLoad = shouldRefreshAfterLoad;
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
                Debug.LogError($"SDK paketleri alınırken hata: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
                EditorApplication.QueuePlayerLoopUpdate();
            }

            if (refreshAfterLoad)
            {
                _ = RefreshPackagesAsync();
            }
        }

        private async Task RefreshPackagesAsync()
        {
            if (_isRefreshing)
            {
                return;
            }

            _isRefreshing = true;
            try
            {
                _packages = await GithubConnector.GetSdkPackagesAsync(true);
                _errorMessage = null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"SDK paket cache yenilemesi başarısız: {ex.Message}");
            }
            finally
            {
                _isRefreshing = false;
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        private async Task DownloadPackageAsync(StarterPackageInfo package)
        {
            if (_isLoading)
            {
                return;
            }

            _isLoading = true;
            DownloadProgressWindow.Controller progressWindow = null;

            try
            {
                progressWindow = DownloadProgressWindow.Show("İndirme", $"{package.Name} indiriliyor...");
                progressWindow.SetIndeterminate();

                bool result = await GithubConnector.DownloadStarterAsync(package.ApiUrl, progressWindow);
                if (!result)
                {
                    EditorUtility.DisplayDialog("İndirme başarısız", $"{package.Name} indirilemedi.", "Tamam");
                }
            }
            finally
            {
                progressWindow?.Close();
                _isLoading = false;
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        public static void ResetCachedData()
        {
            _resetRequested = true;
        }

        public static void RequestReload()
        {
            _reloadRequested = true;
        }

        private void ClearWindowData()
        {
            _initialized = false;
            _isLoading = false;
            _isRefreshing = false;
            _errorMessage = null;
            _scroll = Vector2.zero;
            _packages = null;
        }
    }
}
