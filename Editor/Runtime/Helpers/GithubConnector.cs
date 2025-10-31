using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using static System.IO.Directory;
using static System.IO.Path;

namespace Vida.Framework.Editor
{
    public static class GithubConnector
    {
        #region Private Members
        private static readonly string githubRepoURL = "https://api.github.com/repos/Vida-Tool/packages/contents/";
        private static readonly string githubCommitUrlTemplate = "https://api.github.com/repos/Vida-Tool/packages/commits?path={0}&per_page=1";
        private static string authToken => $"Bearer {ApiKey}";
        private static string acceptToken => "application/vnd.github.v3+json";
        #endregion

        public static int WorkerCount { get; set; } = 0;
        public static bool IsFileDownloading { get; set; } = false;
        private static bool _tasking;
        private static readonly ConcurrentDictionary<string, Task<List<StarterPackageInfo>>> _unityPackageCache = new();
        private static readonly ConcurrentDictionary<string, Task<DateTime?>> _lastUpdatedCache = new();
        private static readonly System.Threading.SemaphoreSlim _commitRequestLimiter = new System.Threading.SemaphoreSlim(4);

        /// <summary>
        /// Bağlantı ayarlarını sıfırlar.
        /// </summary>
        public static void ResetConnection()
        {
            WorkerCount = 0;
            IsFileDownloading = false;
            ClearUnityPackageCache();
        }

        public static void ClearUnityPackageCache()
        {
            _unityPackageCache.Clear();
            _lastUpdatedCache.Clear();
        }

        /// <summary>
        /// GitHub API’ye basit bir bağlantı testi yapar.
        /// </summary>
        /// <returns>Bağlantı başarılı ise true, değilse false döner.</returns>
        public static async Task<bool> TryConnectAsync()
        {
            if (_tasking)
                return false;

            _tasking = true;
            using (UnityWebRequest www = UnityWebRequest.Get(githubRepoURL))
            {
                www.SetRequestHeader("Authorization", authToken);
                www.SetRequestHeader("Accept", acceptToken);
                www.SendWebRequest();

                while (!www.isDone)
                    await Task.Delay(10);

                bool success = www.result == UnityWebRequest.Result.Success;
                Debug.Log("TryConnectAsync result: " + www.result);
                _tasking = false;
                return success;
            }
        }

        /// <summary>
        /// Starter.unitypackage dosyasını indirir ve Unity’ye import eder.
        /// </summary>
        /// <returns>İndirme başarılı ise true döner.</returns>
        /// <summary>
        /// Varsayılan starter paketini indirir.
        /// </summary>
        public static Task<bool> DownloadStarterAsync()
        {
            return DownloadUnityPackageAsync(githubRepoURL + "Starter.unitypackage");
        }

        public static Task<bool> DownloadStarterAsync(IProgress<float> progress)
        {
            return DownloadUnityPackageAsync(githubRepoURL + "Starter.unitypackage", progress);
        }

        /// <summary>
        /// Belirtilen GitHub API URL'sinden starter paketini indirir.
        /// </summary>
        public static Task<bool> DownloadStarterAsync(string apiUrl)
        {
            return DownloadStarterAsync(apiUrl, null);
        }

        /// <summary>
        /// Belirtilen GitHub API URL'sinden starter paketini indirir.
        /// </summary>
        public static Task<bool> DownloadStarterAsync(string apiUrl, IProgress<float> progress)
        {
            string url = string.IsNullOrEmpty(apiUrl)
                ? githubRepoURL + "Starter.unitypackage"
                : apiUrl;

            return DownloadUnityPackageAsync(url, progress);
        }

        /// <summary>
        /// Verilen API url'sindeki unitypackage dosyasını indirir.
        /// </summary>
        public static async Task<bool> DownloadUnityPackageAsync(string apiUrl, IProgress<float> progress = null)
        {
            if (IsFileDownloading) return false;
            if (WorkerCount != 0)
            {
                Debug.Log("Worker is busy");
                return false;
            }

            WorkerCount = 1;
            IsFileDownloading = true;

            try
            {
                progress?.Report(0f);
                using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
                {
                    request.SetRequestHeader("Authorization", authToken);
                    request.SetRequestHeader("Accept", acceptToken);
                    request.SendWebRequest();

                    while (!request.isDone)
                    {
                        progress?.Report(Mathf.Lerp(0f, 0.2f, request.downloadProgress));
                        await Task.Delay(10);
                    }

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError("Failed to download package. Error: " + request.error);
                        return false;
                    }

                    JToken token = JToken.Parse(request.downloadHandler.text);
                    string downloadUrl = token["download_url"]?.ToString();
                    if (string.IsNullOrEmpty(downloadUrl))
                    {
                        Debug.LogError("Download url could not be resolved for package at " + apiUrl + ".");
                        return false;
                    }

                    await ReadUnityPackageAsync(downloadUrl, progress, 0.2f, 0.95f);
                    progress?.Report(1f);
                    Debug.Log("Unity package downloaded successfully!");
                    return true;
                }
            }
            finally
            {
                IsFileDownloading = false;
                WorkerCount = 0;
            }
        }

        /// <summary>
        /// GitHub üzerindeki Starters klasöründeki paket listesini döner.
        /// </summary>
        public static Task<List<StarterPackageInfo>> GetStarterPackagesAsync()
        {
            return GetUnityPackagesAsync("Starters");
        }

        /// <summary>
        /// Belirtilen klasördeki tüm unitypackage dosyalarını döner.
        /// </summary>
        public static Task<List<StarterPackageInfo>> GetUnityPackagesAsync(string relativeDirectory, bool forceRefresh = false)
        {
            if (string.IsNullOrEmpty(relativeDirectory))
            {
                throw new ArgumentException("relativeDirectory");
            }

            return GetUnityPackagesInternalAsync(relativeDirectory, forceRefresh);
        }

        private static Task<List<StarterPackageInfo>> GetUnityPackagesInternalAsync(string relativeDirectory, bool forceRefresh)
        {
            string key = relativeDirectory.Trim('/');

            if (forceRefresh)
            {
                _lastUpdatedCache.Clear();
                Task<List<StarterPackageInfo>> refreshTask = CreateCachedFetchTask(key);
                _unityPackageCache[key] = refreshTask;
                return refreshTask;
            }

            return _unityPackageCache.GetOrAdd(key, _ => CreateCachedFetchTask(key));
        }

        private static Task<List<StarterPackageInfo>> CreateCachedFetchTask(string key)
        {
            return FetchAndTrackAsync();

            async Task<List<StarterPackageInfo>> FetchAndTrackAsync()
            {
                try
                {
                    return await FetchUnityPackagesFromApiCoreAsync(key);
                }
                catch
                {
                    _unityPackageCache.TryRemove(key, out _);
                    throw;
                }
            }
        }

        private static async Task<List<StarterPackageInfo>> FetchUnityPackagesFromApiCoreAsync(string relativeDirectory)
        {
            List<StarterPackageInfo> packages = new List<StarterPackageInfo>();
            string url = githubRepoURL + relativeDirectory.Trim('/');
            await CollectUnityPackagesAsync(url, packages);
            return packages;
        }

        private static async Task CollectUnityPackagesAsync(string apiUrl, List<StarterPackageInfo> packages)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
            {
                request.SetRequestHeader("Authorization", authToken);
                request.SetRequestHeader("Accept", acceptToken);
                request.SendWebRequest();

                while (!request.isDone)
                    await Task.Delay(10);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Failed to fetch packages from " + apiUrl + ". Error: " + request.error);
                    throw new Exception("Paket listesi alınamadı.");
                }

                JArray items = JArray.Parse(request.downloadHandler.text);
                List<Task<StarterPackageInfo>> pendingPackages = new List<Task<StarterPackageInfo>>();

                foreach (JToken item in items)
                {
                    string type = item["type"]?.ToString();
                    if (string.Equals(type, "dir", StringComparison.OrdinalIgnoreCase))
                    {
                        string directoryUrl = item["url"]?.ToString();
                        if (!string.IsNullOrEmpty(directoryUrl))
                        {
                            await CollectUnityPackagesAsync(directoryUrl, packages);
                        }
                        continue;
                    }

                    if (!string.Equals(type, "file", StringComparison.OrdinalIgnoreCase))
                        continue;

                    Task<StarterPackageInfo> infoTask = CreatePackageInfoAsync(item);
                    pendingPackages.Add(infoTask);
                }

                if (pendingPackages.Count > 0)
                {
                    StarterPackageInfo[] results = await Task.WhenAll(pendingPackages);
                    foreach (StarterPackageInfo info in results)
                    {
                        if (info != null)
                        {
                            packages.Add(info);
                        }
                    }
                }
            }
        }

        private static Task<DateTime?> GetFileLastUpdatedCachedAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return Task.FromResult<DateTime?>(null);
            }

            return _lastUpdatedCache.GetOrAdd(path, FetchFileLastUpdatedInternalAsync);
        }

        private static async Task<StarterPackageInfo> CreatePackageInfoAsync(JToken item)
        {
            string name = item["name"]?.ToString();
            if (string.IsNullOrEmpty(name) || !name.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string apiLocation = item["url"]?.ToString();
            string downloadUrl = item["download_url"]?.ToString();
            string path = item["path"]?.ToString();
            string version = StarterPackageInfo.ParseVersion(name);

            DateTime? lastUpdated = null;
            try
            {
                lastUpdated = await GetFileLastUpdatedCachedAsync(path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to resolve last updated information for {path}. Error: {ex.Message}");
            }

            return new StarterPackageInfo(name, version, apiLocation, downloadUrl, lastUpdated);
        }

        private static async Task<DateTime?> FetchFileLastUpdatedInternalAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string escapedPath = UnityWebRequest.EscapeURL(path);
            string url = string.Format(githubCommitUrlTemplate, escapedPath);

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", authToken);
                request.SetRequestHeader("Accept", acceptToken);

                await _commitRequestLimiter.WaitAsync();
                try
                {
                    request.SendWebRequest();

                    while (!request.isDone)
                        await Task.Delay(10);

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning($"Failed to fetch last updated information for {path}. Error: {request.error}");
                        return null;
                    }

                    try
                    {
                        JArray commits = JArray.Parse(request.downloadHandler.text);
                        if (commits.Count == 0)
                            return null;

                        string dateString = commits[0]?["commit"]?["committer"]?["date"]?.ToString()
                                            ?? commits[0]?["commit"]?["author"]?["date"]?.ToString();

                        if (DateTime.TryParse(dateString, out DateTime parsed))
                        {
                            return parsed.ToLocalTime();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to parse last updated information for {path}. Error: {ex.Message}");
                    }

                    return null;
                }
                finally
                {
                    _commitRequestLimiter.Release();
                }
            }
        }

        /// <summary>
        /// İlgili JToken içindeki unitypackage dosyasını indirir ve Unity’ye import eder.
        /// </summary>
        private static async Task ReadUnityPackageAsync(string downloadUrl, IProgress<float> progress = null, float start = 0f, float end = 1f)
        {
            if (string.IsNullOrEmpty(downloadUrl))
            {
                Debug.LogError("Download URL is empty.");
                return;
            }

            string packagePath = "Temp/TempPackage.unitypackage";
            string directoryPath = GetDirectoryName(packagePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Exists(directoryPath))
            {
                CreateDirectory(directoryPath);
            }

            using (UnityWebRequest request = UnityWebRequest.Get(downloadUrl))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SendWebRequest();

                while (!request.isDone)
                {
                    progress?.Report(Mathf.Lerp(start, end, request.downloadProgress));
                    await Task.Delay(20);
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Failed to download unitypackage. Error: " + request.error);
                    return;
                }

                byte[] content = request.downloadHandler.data;
                await File.WriteAllBytesAsync(packagePath, content);
            }

            progress?.Report(end);
            AssetDatabase.ImportPackage(packagePath, true);
            AssetDatabase.Refresh();
            Debug.Log("Package imported successfully!");
        }

        /// <summary>
        /// API anahtarını EditorPrefs üzerinden okur/yazar.
        /// </summary>
        public static string ApiKey
        {
            get => EditorPrefs.GetString("GitApiKey", "");
            set => EditorPrefs.SetString("GitApiKey", value);
        }
    }

    /// <summary>
    /// GitHub Starters klasöründeki bir starter paketine ilişkin temel bilgileri temsil eder.
    /// </summary>
    public class StarterPackageInfo
    {
        public StarterPackageInfo(string name, string version, string apiUrl, string downloadUrl, DateTime? lastUpdated)
        {
            Name = name;
            Version = version;
            ApiUrl = apiUrl;
            DownloadUrl = downloadUrl;
            LastUpdated = lastUpdated;
        }

        public string Name { get; }
        public string Version { get; }
        public string ApiUrl { get; }
        public string DownloadUrl { get; }
        public DateTime? LastUpdated { get; }

        public static string ParseVersion(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrEmpty(nameWithoutExtension))
                return string.Empty;

            string[] parts = nameWithoutExtension.Split('-');
            if (parts.Length < 2)
                return string.Empty;

            return parts[^1];
        }
    }


}
