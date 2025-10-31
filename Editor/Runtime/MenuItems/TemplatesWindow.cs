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

        private const string VidaAssetsFolder = "VidaAssets";
        private const string ThirdPartyFolder = "Third-Part-Assets";

        private static bool _resetRequested;

        private readonly Dictionary<TemplateCategory, List<StarterPackageInfo>> _packages = new();
        private readonly Dictionary<TemplateCategory, string> _errors = new();
        private readonly HashSet<TemplateCategory> _isLoading = new();
        private readonly Dictionary<TemplateCategory, Vector2> _scrollPositions = new();

        private TemplateCategory _activeCategory = TemplateCategory.VidaAssets;
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
            string[] tabs = { "Vida Assets", "Third-Party Assets" };
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            int selected = GUILayout.Toolbar((int)_activeCategory, tabs, GUILayout.Width(Mathf.Min(windowSize.x - 40f, 320f)));
            if (selected != (int)_activeCategory)
            {
                _activeCategory = (TemplateCategory)selected;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawTable(Vector2 windowSize)
        {
            TemplateCategory category = _activeCategory;
            EnsurePackagesLoaded(category);

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            float nameWidth = Mathf.Max(150f, windowSize.x * 0.35f);
            float versionWidth = Mathf.Max(100f, windowSize.x * 0.2f);
            float dateWidth = Mathf.Max(150f, windowSize.x * 0.25f);

            GUILayout.Label("Paket adı", GUILayout.Width(nameWidth));
            GUILayout.Label("Versiyon numarası", GUILayout.Width(versionWidth));
            GUILayout.Label("Son güncellenme", GUILayout.Width(dateWidth));
            GUILayout.FlexibleSpace();
            GUILayout.Label("İndirme", GUILayout.Width(100f));
            GUILayout.EndHorizontal();

            if (_isLoading.Contains(category))
            {
                GUILayout.Label("Paketler yükleniyor...");
            }
            else if (_errors.TryGetValue(category, out string error) && !string.IsNullOrEmpty(error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
                if (GUILayout.Button("Tekrar Dene", GUILayout.Width(120f)))
                {
                    _ = LoadPackagesAsync(category, true);
                }
            }
            else if (_packages.TryGetValue(category, out List<StarterPackageInfo> packages) && packages.Count > 0)
            {
                Vector2 scroll = _scrollPositions.TryGetValue(category, out Vector2 existing) ? existing : Vector2.zero;
                scroll = GUILayout.BeginScrollView(scroll);
                foreach (StarterPackageInfo package in packages)
                {
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.Label(package.Name, GUILayout.Width(nameWidth));
                    GUILayout.Label(string.IsNullOrEmpty(package.Version) ? "-" : package.Version, GUILayout.Width(versionWidth));
                    string updatedText = package.LastUpdated.HasValue
                        ? package.LastUpdated.Value.ToString("dd.MM.yyyy HH:mm")
                        : "-";
                    GUILayout.Label(updatedText, GUILayout.Width(dateWidth));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("İndir", GUILayout.Width(100f)))
                    {
                        DownloadTemplate(package);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
                _scrollPositions[category] = scroll;
            }
            else
            {
                GUILayout.Label("Gösterilecek paket bulunamadı.");
            }

            GUILayout.EndVertical();
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
                List<StarterPackageInfo> packages = await GithubConnector.GetUnityPackagesAsync(directory);
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
