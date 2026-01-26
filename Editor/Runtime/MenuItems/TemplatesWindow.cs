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
        private enum TemplateCategory
        {
            VidaAssets = 0,
            ThirdParty = 1
        }

        private enum TemplateFilter
        {
            All = 0,
            VidaAssets = 1,
            ThirdParty = 2
        }

        private const string VidaAssetsFolder = "Vida-Assets";
        private const string ThirdPartyFolder = "Third-Party-Assets";


        private static bool _resetRequested;

        private readonly Dictionary<TemplateCategory, List<StarterPackageInfo>> _packages = new();
        private readonly Dictionary<TemplateCategory, string> _errors = new();
        private readonly HashSet<TemplateCategory> _isLoading = new();
        private readonly Dictionary<TemplateFilter, Vector2> _scrollPositions = new();

        private TemplateFilter _activeFilter = TemplateFilter.All;
        private string _activePackageCategory = "All";
        private bool _isDownloading;

        public void Draw(Vector2 windowSize)
        {
            if (_resetRequested)
            {
                ClearCachedData();
                _resetRequested = false;
            }

            DrawTabs(windowSize);
            GUILayout.Space(10f);
            DrawTable(windowSize);
        }

        private void DrawTabs(Vector2 windowSize)
        {
            string[] tabs = { "All", "Vida", "Third-party" };
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            int selected = GUILayout.Toolbar((int)_activeFilter, tabs, GUILayout.Width(Mathf.Min(windowSize.x - 40f, 360f)));
            if (selected != (int)_activeFilter)
            {
                _activeFilter = (TemplateFilter)selected;
                _activePackageCategory = "All";
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawTable(Vector2 windowSize)
        {
            EnsurePackagesLoadedForFilter(_activeFilter);

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            StarterPackageInfoExtensions.GetColumnWidths(windowSize.x, out float categoryWidth, out float nameWidth,
                out float versionWidth, out float dateWidth, out float downloadWidth);

            GUILayout.Label("Kategori", GUILayout.Width(categoryWidth));
            GUILayout.Label("Paket adı", GUILayout.Width(nameWidth));
            GUILayout.Label("Versiyon numarası", GUILayout.Width(versionWidth));
            GUILayout.Label("Son güncellenme", GUILayout.Width(dateWidth));
            GUILayout.FlexibleSpace();
            GUILayout.Label("İndirme", GUILayout.Width(downloadWidth));
            GUILayout.EndHorizontal();

            if (IsLoadingForFilter(_activeFilter))
            {
                GUILayout.Label("Paketler yükleniyor...");
            }
            else if (TryGetErrorForFilter(_activeFilter, out string error) && !string.IsNullOrEmpty(error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
                if (GUILayout.Button("Tekrar Dene", GUILayout.Width(120f)))
                {
                    _ = ReloadPackagesForFilterAsync(_activeFilter);
                }
            }
            else if (TryGetPackagesForFilter(_activeFilter, out List<StarterPackageInfo> packages) && packages.Count > 0)
            {
                string searchText = MainToolbar.search?.Trim();
                List<string> packageCategories = packages
                    .Select(p => p.GetDisplayInfo().Category)
                    .Where(category => !string.IsNullOrEmpty(category))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                DrawCategoryFilters(windowSize, packageCategories);

                IEnumerable<StarterPackageInfo> filteredPackages = string.IsNullOrEmpty(searchText)
                    ? packages
                    : packages.Where(p => p.MatchesSearch(searchText));

                if (!string.Equals(_activePackageCategory, "All", StringComparison.OrdinalIgnoreCase))
                {
                    filteredPackages = filteredPackages.Where(p =>
                        string.Equals(p.GetDisplayInfo().Category, _activePackageCategory, StringComparison.OrdinalIgnoreCase));
                }

                List<StarterPackageInfo> filteredList = filteredPackages.ToList();

                if (filteredList.Count == 0)
                {
                    GUILayout.Label("Arama kriterine uygun paket bulunamadı.");
                    GUILayout.EndVertical();
                    return;
                }

                TemplateFilter filterKey = _activeFilter;
                Vector2 scroll = _scrollPositions.TryGetValue(filterKey, out Vector2 existing) ? existing : Vector2.zero;
                scroll = GUILayout.BeginScrollView(scroll);
                foreach (StarterPackageInfo package in filteredList)
                {
                    PackageDisplayInfo displayInfo = package.GetDisplayInfo();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.Label(displayInfo.Category, GUILayout.Width(categoryWidth));
                    GUILayout.Label(displayInfo.Name, GUILayout.Width(nameWidth));
                    GUILayout.Label(string.IsNullOrEmpty(displayInfo.Version) ? "-" : displayInfo.Version, GUILayout.Width(versionWidth));
                    string updatedText = package.LastUpdated.HasValue
                        ? package.LastUpdated.Value.ToString("dd.MM.yyyy HH:mm")
                        : "-";
                    GUILayout.Label(updatedText, GUILayout.Width(dateWidth));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("İndir", GUILayout.Width(downloadWidth)))
                    {
                        DownloadTemplate(package);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
                _scrollPositions[filterKey] = scroll;
            }
            else
            {
                GUILayout.Label("Gösterilecek paket bulunamadı.");
            }

            GUILayout.EndVertical();
        }

        private void EnsurePackagesLoadedForFilter(TemplateFilter filter)
        {
            switch (filter)
            {
                case TemplateFilter.VidaAssets:
                    EnsurePackagesLoaded(TemplateCategory.VidaAssets);
                    break;
                case TemplateFilter.ThirdParty:
                    EnsurePackagesLoaded(TemplateCategory.ThirdParty);
                    break;
                default:
                    EnsurePackagesLoaded(TemplateCategory.VidaAssets);
                    EnsurePackagesLoaded(TemplateCategory.ThirdParty);
                    break;
            }
        }

        private void EnsurePackagesLoaded(TemplateCategory category)
        {
            if (_packages.ContainsKey(category) || _isLoading.Contains(category))
            {
                return;
            }

            _ = LoadPackagesAsync(category, false);
        }

        private async Task LoadPackagesAsync(TemplateCategory category, bool force)
        {
            if (_isLoading.Contains(category))
            {
                return;
            }

            if (force)
            {
                _packages.Remove(category);
            }

            _isLoading.Add(category);
            _errors.Remove(category);

            try
            {
                string directory = category == TemplateCategory.VidaAssets ? VidaAssetsFolder : ThirdPartyFolder;
                List<StarterPackageInfo> packages = await GithubConnector.GetUnityPackagesAsync(directory, force);
                packages = packages
                    .OrderByDescending(p => p.LastUpdated ?? DateTime.MinValue)
                    .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                _packages[category] = packages;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Paketler yüklenirken hata oluştu: {ex.Message}");
                _errors[category] = "Paket listesi alınamadı.";
            }
            finally
            {
                _isLoading.Remove(category);
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        private bool IsLoadingForFilter(TemplateFilter filter)
        {
            return filter switch
            {
                TemplateFilter.VidaAssets => _isLoading.Contains(TemplateCategory.VidaAssets),
                TemplateFilter.ThirdParty => _isLoading.Contains(TemplateCategory.ThirdParty),
                _ => _isLoading.Contains(TemplateCategory.VidaAssets) || _isLoading.Contains(TemplateCategory.ThirdParty)
            };
        }

        private bool TryGetErrorForFilter(TemplateFilter filter, out string error)
        {
            error = null;
            return filter switch
            {
                TemplateFilter.VidaAssets => _errors.TryGetValue(TemplateCategory.VidaAssets, out error),
                TemplateFilter.ThirdParty => _errors.TryGetValue(TemplateCategory.ThirdParty, out error),
                _ => _errors.TryGetValue(TemplateCategory.VidaAssets, out error)
                     || _errors.TryGetValue(TemplateCategory.ThirdParty, out error)
            };
        }

        private bool TryGetPackagesForFilter(TemplateFilter filter, out List<StarterPackageInfo> packages)
        {
            packages = null;
            return filter switch
            {
                TemplateFilter.VidaAssets => _packages.TryGetValue(TemplateCategory.VidaAssets, out packages),
                TemplateFilter.ThirdParty => _packages.TryGetValue(TemplateCategory.ThirdParty, out packages),
                _ => TryGetCombinedPackages(out packages)
            };
        }

        private bool TryGetCombinedPackages(out List<StarterPackageInfo> packages)
        {
            packages = new List<StarterPackageInfo>();
            if (_packages.TryGetValue(TemplateCategory.VidaAssets, out List<StarterPackageInfo> vidaPackages))
            {
                packages.AddRange(vidaPackages);
            }

            if (_packages.TryGetValue(TemplateCategory.ThirdParty, out List<StarterPackageInfo> thirdPartyPackages))
            {
                packages.AddRange(thirdPartyPackages);
            }

            if (packages.Count == 0)
            {
                packages = null;
                return false;
            }

            return true;
        }

        private async Task ReloadPackagesForFilterAsync(TemplateFilter filter)
        {
            switch (filter)
            {
                case TemplateFilter.VidaAssets:
                    await LoadPackagesAsync(TemplateCategory.VidaAssets, true);
                    break;
                case TemplateFilter.ThirdParty:
                    await LoadPackagesAsync(TemplateCategory.ThirdParty, true);
                    break;
                default:
                    await LoadPackagesAsync(TemplateCategory.VidaAssets, true);
                    await LoadPackagesAsync(TemplateCategory.ThirdParty, true);
                    break;
            }
        }

        private void DrawCategoryFilters(Vector2 windowSize, List<string> categories)
        {
            if (categories == null)
            {
                return;
            }

            List<string> options = new List<string> { "All" };
            options.AddRange(categories);

            if (!options.Contains(_activePackageCategory, StringComparer.OrdinalIgnoreCase))
            {
                _activePackageCategory = "All";
            }

            int selectedIndex = options.FindIndex(option =>
                string.Equals(option, _activePackageCategory, StringComparison.OrdinalIgnoreCase));

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            int nextIndex = GUILayout.Toolbar(selectedIndex, options.ToArray(), GUILayout.Width(Mathf.Min(windowSize.x - 40f, 480f)));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (nextIndex != selectedIndex && nextIndex >= 0 && nextIndex < options.Count)
            {
                _activePackageCategory = options[nextIndex];
            }

            GUILayout.Space(6f);
        }

        private async void DownloadTemplate(StarterPackageInfo package)
        {
            if (_isDownloading || package == null)
            {
                return;
            }

            _isDownloading = true;
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
                _isDownloading = false;
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        private void ClearCachedData()
        {
            _packages.Clear();
            _errors.Clear();
            _isLoading.Clear();
            _scrollPositions.Clear();
            _isDownloading = false;
        }

        public static void ResetCachedData()
        {
            _resetRequested = true;
        }
    }
}
