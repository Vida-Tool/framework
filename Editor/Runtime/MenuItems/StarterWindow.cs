using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public class StarterWindow
    {
        private bool _initialized;
        private bool _isLoading;
        private string _errorMessage;
        private Vector2 _scroll;
        private List<StarterPackageInfo> _packages;

        public void Draw(Vector2 windowSize)
        {
            if (!_initialized && !_isLoading)
            {
                _initialized = true;
                _ = LoadPackagesAsync();
            }

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            float nameWidth = Mathf.Max(150f, windowSize.x * 0.35f);
            float versionWidth = Mathf.Max(100f, windowSize.x * 0.2f);
            float dateWidth = Mathf.Max(150f, windowSize.x * 0.25f);

            GUILayout.Label("Paket adı", GUILayout.Width(nameWidth));
            GUILayout.Label("Versiyon numarası", GUILayout.Width(versionWidth));
            GUILayout.Label("Son güncellenme", GUILayout.Width(dateWidth));
            GUILayout.FlexibleSpace();
            GUILayout.Label("İndirme", GUILayout.Width(100));
            GUILayout.EndHorizontal();

            if (_isLoading)
            {
                GUILayout.Label("Starter paketleri yükleniyor...");
            }
            else if (!string.IsNullOrEmpty(_errorMessage))
            {
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
                if (GUILayout.Button("Tekrar Dene"))
                {
                    _ = LoadPackagesAsync();
                }
            }
            else if (_packages is { Count: > 0 })
            {
                _scroll = GUILayout.BeginScrollView(_scroll);
                foreach (StarterPackageInfo package in _packages)
                {
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.Label(package.Name, GUILayout.Width(nameWidth));
                    GUILayout.Label(string.IsNullOrEmpty(package.Version) ? "-" : package.Version, GUILayout.Width(versionWidth));
                    string updatedText = package.LastUpdated.HasValue
                        ? package.LastUpdated.Value.ToString("dd.MM.yyyy HH:mm")
                        : "-";
                    GUILayout.Label(updatedText, GUILayout.Width(dateWidth));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("İndir", GUILayout.Width(100)))
                    {
                        _ = DownloadPackageAsync(package);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("Gösterilecek starter paketi bulunamadı.");
                if (GUILayout.Button("Yenile"))
                {
                    _ = LoadPackagesAsync();
                }
            }

            GUILayout.EndVertical();
        }

        private async Task LoadPackagesAsync()
        {
            if (_isLoading)
            {
                return;
            }
            _isLoading = true;
            _errorMessage = null;
            try
            {
                _packages = await GithubConnector.GetStarterPackagesAsync();
            }
            catch (System.Exception ex)
            {
                _errorMessage = ex.Message;
                Debug.LogError($"Starter paketleri alınırken hata: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
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
    }
}