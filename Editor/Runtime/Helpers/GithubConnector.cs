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
        private const string PackageCacheKeyPrefix = "VidaFramework.UnityPackageCache.";
        private const string PackageCacheIndexKey = "VidaFramework.UnityPackageCache.Keys";
        private const string PackageDefaultBranchCacheKey = "VidaFramework.PackageDefaultBranch";
        private const string ApiKeyPrefsKey = "GitApiKey";
        private static readonly string githubRepoApiURL = "https://api.github.com/repos/Vida-Tool/packages";
        private static readonly string githubRepoURL = "https://api.github.com/repos/Vida-Tool/packages/contents/";
        private static readonly string githubTreeUrlTemplate = "https://api.github.com/repos/Vida-Tool/packages/git/trees/{0}?recursive=1";
        private static string authToken => $"Bearer {ApiKey}";
        private static string acceptToken => "application/vnd.github.v3+json";
        #endregion

        public static int WorkerCount { get; set; } = 0;
        public static bool IsFileDownloading { get; set; } = false;
        private static bool _tasking;
        private static readonly ConcurrentDictionary<string, Task<List<StarterPackageInfo>>> _unityPackageCache = new();
        private static Task<List<StarterPackageInfo>> _packageTreeFetchTask;

        /// <summary>
        /// Bağlantı ayarlarını sıfırlar.
        /// </summary>
        public static void ResetConnection()
        {
            WorkerCount = 0;
            IsFileDownloading = false;
            ClearUnityPackageCache();
        }

        public static void ClearUnityPackageCache(bool clearPersistentCache = false)
        {
            _unityPackageCache.Clear();
            _packageTreeFetchTask = null;
            if (clearPersistentCache)
            {
                ClearPersistentUnityPackageCache();
            }
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

        public static Task<List<StarterPackageInfo>> GetStarterPackagesAsync(bool forceRefresh)
        {
            return GetUnityPackagesAsync("Starters", forceRefresh);
        }

        /// <summary>
        /// GitHub üzerindeki Sdk klasöründeki paket listesini döner.
        /// </summary>
        public static Task<List<StarterPackageInfo>> GetSdkPackagesAsync()
        {
            return GetUnityPackagesAsync("Sdk");
        }

        public static Task<List<StarterPackageInfo>> GetSdkPackagesAsync(bool forceRefresh)
        {
            return GetUnityPackagesAsync("Sdk", forceRefresh);
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

        public static bool HasPersistentUnityPackageCache(string relativeDirectory)
        {
            if (string.IsNullOrEmpty(relativeDirectory))
            {
                return false;
            }

            string key = NormalizePackageCacheKey(relativeDirectory);
            return EditorPrefs.HasKey(GetPersistentPackageCacheKey(key));
        }

        private static Task<List<StarterPackageInfo>> GetUnityPackagesInternalAsync(string relativeDirectory, bool forceRefresh)
        {
            string key = NormalizePackageCacheKey(relativeDirectory);

            if (forceRefresh)
            {
                Task<List<StarterPackageInfo>> refreshTask = CreateCachedFetchTask(key, true);
                _unityPackageCache[key] = refreshTask;
                return refreshTask;
            }

            if (_unityPackageCache.TryGetValue(key, out Task<List<StarterPackageInfo>> cachedTask))
            {
                return cachedTask;
            }

            if (TryLoadPersistentUnityPackageCache(key, out List<StarterPackageInfo> cachedPackages))
            {
                Task<List<StarterPackageInfo>> persistentTask = Task.FromResult(cachedPackages);
                _unityPackageCache[key] = persistentTask;
                return persistentTask;
            }

            return _unityPackageCache.GetOrAdd(key, _ => CreateCachedFetchTask(key, false));
        }

        private static Task<List<StarterPackageInfo>> CreateCachedFetchTask(string key, bool forceRefresh)
        {
            return FetchAndTrackAsync();

            async Task<List<StarterPackageInfo>> FetchAndTrackAsync()
            {
                try
                {
                    List<StarterPackageInfo> packages = await FetchUnityPackagesFromTreeAsync(key, forceRefresh);
                    SavePersistentUnityPackageCache(key, packages);
                    return packages;
                }
                catch
                {
                    _unityPackageCache.TryRemove(key, out _);
                    throw;
                }
            }
        }

        private static async Task<List<StarterPackageInfo>> FetchUnityPackagesFromTreeAsync(string relativeDirectory, bool forceRefresh)
        {
            List<StarterPackageInfo> packages = new List<StarterPackageInfo>();
            List<StarterPackageInfo> treePackages = await GetPackageTreePackagesAsync(forceRefresh);
            string directory = NormalizePackageCacheKey(relativeDirectory);

            for (int i = 0; i < treePackages.Count; i++)
            {
                StarterPackageInfo package = treePackages[i];
                if (IsPackageInDirectory(package.RelativePath, directory))
                {
                    packages.Add(package);
                }
            }

            return packages;
        }

        private static Task<List<StarterPackageInfo>> GetPackageTreePackagesAsync(bool forceRefresh)
        {
            if (_packageTreeFetchTask == null || _packageTreeFetchTask.IsFaulted || _packageTreeFetchTask.IsCanceled || (forceRefresh && _packageTreeFetchTask.IsCompleted))
            {
                _packageTreeFetchTask = FetchPackageTreePackagesAsync();
            }

            return _packageTreeFetchTask;
        }

        private static async Task<List<StarterPackageInfo>> FetchPackageTreePackagesAsync()
        {
            string branch = await GetDefaultBranchAsync();
            string url = string.Format(githubTreeUrlTemplate, UnityWebRequest.EscapeURL(branch));
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", authToken);
                request.SetRequestHeader("Accept", acceptToken);
                request.SendWebRequest();

                while (!request.isDone)
                    await Task.Delay(10);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Failed to fetch package tree. Error: " + request.error);
                    throw new Exception("Paket ağacı alınamadı.");
                }

                JObject root = JObject.Parse(request.downloadHandler.text);
                if (root["truncated"]?.Value<bool>() == true)
                {
                    Debug.LogWarning("GitHub package tree response was truncated. Some packages may be missing.");
                }

                JArray tree = root["tree"] as JArray;
                return CreatePackageInfosFromTree(tree, branch);
            }
        }

        private static async Task<string> GetDefaultBranchAsync()
        {
            string cachedBranch = EditorPrefs.GetString(PackageDefaultBranchCacheKey, string.Empty);
            if (!string.IsNullOrEmpty(cachedBranch))
            {
                return cachedBranch;
            }

            using (UnityWebRequest request = UnityWebRequest.Get(githubRepoApiURL))
            {
                request.SetRequestHeader("Authorization", authToken);
                request.SetRequestHeader("Accept", acceptToken);
                request.SendWebRequest();

                while (!request.isDone)
                    await Task.Delay(10);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("Failed to resolve default branch. Falling back to main. Error: " + request.error);
                    return "main";
                }

                JObject root = JObject.Parse(request.downloadHandler.text);
                string branch = root["default_branch"]?.ToString();
                if (string.IsNullOrEmpty(branch))
                {
                    return "main";
                }

                EditorPrefs.SetString(PackageDefaultBranchCacheKey, branch);
                return branch;
            }
        }

        private static List<StarterPackageInfo> CreatePackageInfosFromTree(JArray tree, string branch)
        {
            List<StarterPackageInfo> packages = new List<StarterPackageInfo>();
            if (tree == null)
            {
                return packages;
            }

            foreach (JToken item in tree)
            {
                StarterPackageInfo package = CreatePackageInfoFromTreeItem(item, branch);
                if (package != null)
                {
                    packages.Add(package);
                }
            }

            return packages;
        }

        private static StarterPackageInfo CreatePackageInfoFromTreeItem(JToken item, string branch)
        {
            string type = item["type"]?.ToString();
            if (!string.Equals(type, "blob", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string path = item["path"]?.ToString();
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string name = System.IO.Path.GetFileName(path);
            string version = StarterPackageInfo.ParseVersion(name);
            string apiLocation = CreateContentsApiUrl(path, branch);
            string downloadUrl = CreateRawDownloadUrl(path, branch);

            return new StarterPackageInfo(name, version, apiLocation, downloadUrl, path);
        }

        private static string NormalizePackageCacheKey(string relativeDirectory)
        {
            return relativeDirectory.Trim('/');
        }

        private static bool TryLoadPersistentUnityPackageCache(string key, out List<StarterPackageInfo> packages)
        {
            packages = null;
            string cacheKey = GetPersistentPackageCacheKey(key);
            if (!EditorPrefs.HasKey(cacheKey))
            {
                return false;
            }

            string json = EditorPrefs.GetString(cacheKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                JArray cachedItems = JArray.Parse(json);
                packages = new List<StarterPackageInfo>();
                foreach (JToken item in cachedItems)
                {
                    string name = item["name"]?.ToString();
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    string version = item["version"]?.ToString();
                    string apiUrl = item["apiUrl"]?.ToString();
                    string downloadUrl = item["downloadUrl"]?.ToString();
                    string path = item["path"]?.ToString();
                    packages.Add(new StarterPackageInfo(name, version, apiUrl, downloadUrl, path));
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to read cached package list for {key}. Error: {ex.Message}");
                EditorPrefs.DeleteKey(cacheKey);
                return false;
            }
        }

        private static void SavePersistentUnityPackageCache(string key, List<StarterPackageInfo> packages)
        {
            if (packages == null)
            {
                return;
            }

            JArray cachedItems = new JArray();
            foreach (StarterPackageInfo package in packages)
            {
                JObject item = new JObject
                {
                    ["name"] = package.Name,
                    ["version"] = package.Version,
                    ["apiUrl"] = package.ApiUrl,
                    ["downloadUrl"] = package.DownloadUrl,
                    ["path"] = package.RelativePath
                };
                cachedItems.Add(item);
            }

            EditorPrefs.SetString(GetPersistentPackageCacheKey(key), cachedItems.ToString());
            RegisterPersistentPackageCacheKey(key);
        }

        private static void ClearPersistentUnityPackageCache()
        {
            List<string> keys = GetPersistentPackageCacheKeys();
            for (int i = 0; i < keys.Count; i++)
            {
                EditorPrefs.DeleteKey(GetPersistentPackageCacheKey(keys[i]));
            }

            EditorPrefs.DeleteKey(PackageCacheIndexKey);
            EditorPrefs.DeleteKey(PackageDefaultBranchCacheKey);
        }

        private static void RegisterPersistentPackageCacheKey(string key)
        {
            List<string> keys = GetPersistentPackageCacheKeys();
            if (!keys.Contains(key))
            {
                keys.Add(key);
                EditorPrefs.SetString(PackageCacheIndexKey, string.Join("\n", keys));
            }
        }

        private static List<string> GetPersistentPackageCacheKeys()
        {
            List<string> keys = new List<string>();
            string value = EditorPrefs.GetString(PackageCacheIndexKey, string.Empty);
            if (string.IsNullOrEmpty(value))
            {
                return keys;
            }

            string[] splitKeys = value.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < splitKeys.Length; i++)
            {
                keys.Add(splitKeys[i]);
            }

            return keys;
        }

        private static string GetPersistentPackageCacheKey(string key)
        {
            return PackageCacheKeyPrefix + key.Replace("/", ".");
        }

        private static bool IsPackageInDirectory(string path, string directory)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(directory))
            {
                return false;
            }

            return path.StartsWith(directory + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static string CreateContentsApiUrl(string path, string branch)
        {
            return githubRepoURL + EscapeGithubPath(path) + "?ref=" + UnityWebRequest.EscapeURL(branch);
        }

        private static string CreateRawDownloadUrl(string path, string branch)
        {
            return "https://raw.githubusercontent.com/Vida-Tool/packages/" + UnityWebRequest.EscapeURL(branch) + "/" + EscapeGithubPath(path);
        }

        private static string EscapeGithubPath(string path)
        {
            string[] parts = path.Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = UnityWebRequest.EscapeURL(parts[i]);
            }

            return string.Join("/", parts);
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
            get => EditorPrefs.GetString(ApiKeyPrefsKey, "");
            set => EditorPrefs.SetString(ApiKeyPrefsKey, value);
        }

        public static void ClearApiKey()
        {
            EditorPrefs.DeleteKey(ApiKeyPrefsKey);
        }
    }

    /// <summary>
    /// GitHub Starters klasöründeki bir starter paketine ilişkin temel bilgileri temsil eder.
    /// </summary>
    public class StarterPackageInfo
    {
        public StarterPackageInfo(string name, string version, string apiUrl, string downloadUrl, string relativePath = null)
        {
            Name = name;
            Version = version;
            ApiUrl = apiUrl;
            DownloadUrl = downloadUrl;
            RelativePath = relativePath;
        }

        public string Name { get; }
        public string Version { get; }
        public string ApiUrl { get; }
        public string DownloadUrl { get; }
        public string RelativePath { get; }

        public static string ParseVersion(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            string nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrEmpty(nameWithoutExtension))
                return string.Empty;

            string[] parts = nameWithoutExtension.Split('-');
            if (parts.Length < 2)
                return string.Empty;

            return parts[^1];
        }
    }


}
