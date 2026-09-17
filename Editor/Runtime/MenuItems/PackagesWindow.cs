using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public class PackagesWindow
    {
        private static bool _resetRequested;
        private static bool _reloadRequested;

        private bool _initialized;
        private bool _isLoading;
        private string _errorMessage;
        private Vector2 _scroll;
        private List<StarterPackageInfo> _packages;
        private readonly PackageListView _packageList = new PackageListView();

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

            VidaPremiumGUI.DrawPackageTableHeader(windowSize.x);
            GUILayout.Space(6f);

            if (_isLoading)
            {
                VidaPremiumGUI.DrawCenteredState(
                    "Paketler yükleniyor...",
                    "Yayınlanmış Framework paketleri hazırlanıyor.",
                    VidaPremiumGUI.GetPremiumTexture("status-refreshing.png"));
            }
            else if (!string.IsNullOrEmpty(_errorMessage))
            {
                if (VidaPremiumGUI.DrawRetryState("Framework paketleri alınamadı.", _errorMessage))
                {
                    _ = LoadPackagesAsync(true);
                }
            }
            else if (_packages is { Count: > 0 })
            {
                _scroll = GUILayout.BeginScrollView(_scroll);
                foreach (StarterPackageInfo package in _packages)
                {
                    _packageList.DrawPackage(package, windowSize.x, _isLoading);

                    GUILayout.Space(6f);
                }

                GUILayout.EndScrollView();
            }
            else
            {
                VidaPremiumGUI.DrawCenteredState(
                    "Gösterilecek Framework paketi bulunamadı.",
                    "Kataloğu yenileyerek tekrar deneyebilirsin.",
                    VidaPremiumGUI.GetPremiumTexture("icon-download.png"));
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
                FrameworkSession.SessionSnapshot session = FrameworkSession.Capture();
                List<StarterPackageInfo> framework = await FrameworkStoreClient.GetPackagesAsync("framework", forceRefresh);
                List<StarterPackageInfo> general = await FrameworkStoreClient.GetPackagesAsync("package");
                FrameworkSession.EnsureCurrent(session);
                _packages = framework
                    .Concat(general)
                    .OrderBy(package => package.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (Exception exception)
            {
                _errorMessage = exception.Message;
                Debug.LogError("Framework paketleri alınırken hata: " + exception.Message);
            }
            finally
            {
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
            _errorMessage = null;
            _scroll = Vector2.zero;
            _packages = null;
            _packageList.Reset();
        }
    }
}
