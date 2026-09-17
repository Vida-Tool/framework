using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public class TemplatesWindow
    {
        private static bool _resetRequested;
        private static bool _reloadRequested;

        private bool _initialized;
        private bool _isLoading;
        private bool _isDownloading;
        private string _errorMessage;
        private Vector2 _scroll;
        private List<StarterPackageInfo> _packages;

        public void Draw(Vector2 windowSize)
        {
            if (_resetRequested)
            {
                ClearCachedData();
                _resetRequested = false;
            }

            if (_reloadRequested)
            {
                ClearCachedData();
                _reloadRequested = false;
                _initialized = true;
                _ = LoadPackagesAsync(true);
            }

            if (!_initialized && !_isLoading)
            {
                _initialized = true;
                _ = LoadPackagesAsync(false);
            }

            DrawHeader(windowSize);
            GUILayout.Space(10f);
            VidaPremiumGUI.DrawPackageTableHeader(windowSize.x);
            GUILayout.Space(6f);

            if (_isLoading)
            {
                VidaPremiumGUI.DrawCenteredState(
                    "Paketler yükleniyor...",
                    "Yayınlanmış template paketleri hazırlanıyor.",
                    VidaPremiumGUI.GetPremiumTexture("status-refreshing.png"));
                return;
            }

            if (!string.IsNullOrEmpty(_errorMessage))
            {
                if (VidaPremiumGUI.DrawRetryState("Paket listesi alınamadı.", _errorMessage))
                {
                    _ = LoadPackagesAsync(true);
                }

                return;
            }

            string search = MainToolbar.search?.Trim();
            List<StarterPackageInfo> filtered = _packages?
                .Where(package => package.MatchesSearch(search))
                .ToList();
            if (filtered == null || filtered.Count == 0)
            {
                VidaPremiumGUI.DrawCenteredState(
                    "Gösterilecek template paketi bulunamadı.",
                    "Arama kriterini değiştir veya kataloğu yenile.",
                    VidaPremiumGUI.GetPremiumTexture("icon-templates.png"));
                return;
            }

            _scroll = GUILayout.BeginScrollView(_scroll, false, false);
            foreach (StarterPackageInfo package in filtered)
            {
                if (VidaPremiumGUI.DrawPackageRow(package.GetDisplayInfo(), windowSize.x, _isDownloading))
                {
                    PackageDetailsWindow.Open(package);
                }

                GUILayout.Space(6f);
            }

            GUILayout.EndScrollView();
        }

        private static void DrawHeader(Vector2 windowSize)
        {
            using (new GUILayout.HorizontalScope())
            {
                using (new GUILayout.VerticalScope())
                {
                    VidaPremiumGUI.DrawHeaderInfo("Templates", "Published template packages from the Vida Framework catalog.");
                }

                GUILayout.FlexibleSpace();
                GUILayout.Space(12f);
                float width = Mathf.Min(280f, Mathf.Max(180f, windowSize.x * 0.34f));
                using (new GUILayout.VerticalScope(GUILayout.Width(width)))
                {
                    GUILayout.Space(2f);
                    MainToolbar.search = VidaPremiumGUI.DrawSearchField(MainToolbar.search, width);
                }
            }
        }

        private async Task LoadPackagesAsync(bool forceRefresh)
        {
            if (_isLoading)
            {
                return;
            }

            _isLoading = true;
            _errorMessage = null;
            try
            {
                _packages = await FrameworkStoreClient.GetPackagesAsync("template", forceRefresh);
            }
            catch (Exception exception)
            {
                _errorMessage = exception.Message;
                Debug.LogError("Template paketleri alınırken hata: " + exception.Message);
            }
            finally
            {
                _isLoading = false;
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        private async Task DownloadTemplateAsync(StarterPackageInfo package)
        {
            if (_isDownloading)
            {
                return;
            }

            _isDownloading = true;
            DownloadProgressWindow.Controller progressWindow = null;
            try
            {
                progressWindow = DownloadProgressWindow.Show("İndirme", package.Name + " indiriliyor...");
                progressWindow.SetIndeterminate();
                await FrameworkStoreClient.DownloadAndImportAsync(package, progressWindow);
            }
            catch (Exception exception)
            {
                Debug.LogError("Template paketi indirilemedi: " + exception.Message);
                EditorUtility.DisplayDialog("İndirme başarısız", exception.Message, "Tamam");
            }
            finally
            {
                progressWindow?.Close();
                _isDownloading = false;
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

        private void ClearCachedData()
        {
            _initialized = false;
            _isLoading = false;
            _isDownloading = false;
            _errorMessage = null;
            _scroll = Vector2.zero;
            _packages = null;
        }
    }
}
