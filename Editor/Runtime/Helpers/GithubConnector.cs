using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using static System.IO.Directory;
using static System.IO.Path;
using static UnityEngine.Application;

namespace Vida.Framework.Editor
{
    public static class GithubConnector
    {
        public static List<VidaAssetCollection> AssetCollections;

        #region Private Members
        private static readonly string githubRepoURL = "https://api.github.com/repos/Vida-Tool/packages/contents/";
        private static string authToken => $"Bearer {ApiKey}";
        private static string acceptToken => "application/vnd.github.v3+json";
        #endregion

        public static int WorkerCount { get; set; } = 0;
        public static bool IsFileReading { get; set; } = false;
        public static bool IsFileDownloading { get; set; } = false;
        private static bool _tasking;

        /// <summary>
        /// AssetCollections listesini JSON olarak EditorPrefs’e kaydeder.
        /// </summary>
        public static async Task SaveAssetCollectionsAsync()
        {
            // Dosya okuma işlemi tamamlanana kadar bekle
            while (IsFileReading && (AssetCollections == null || AssetCollections.Count <= 0))
            {
                await Task.Delay(10);
            }
            string json = JsonConvert.SerializeObject(AssetCollections);
            EditorPrefs.SetString("Collections", json);
        }

        /// <summary>
        /// EditorPrefs’den JSON olarak kaydedilmiş AssetCollections listesini okur.
        /// </summary>
        public static List<VidaAssetCollection> LoadAssetCollections()
        {
            if (EditorPrefs.HasKey("Collections"))
            {
                string json = EditorPrefs.GetString("Collections", null);
                try
                {
                    return JsonConvert.DeserializeObject<List<VidaAssetCollection>>(json);
                }
                catch (Exception ex)
                {
                    Debug.LogError("LoadAssetCollections hatası: " + ex.Message);
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Bağlantı ayarlarını sıfırlar.
        /// </summary>
        public static void ResetConnection()
        {
            WorkerCount = 0;
            IsFileReading = false;
            IsFileDownloading = false;
            EditorPrefs.DeleteKey("Collections");
            AssetCollections = null;
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
        /// Asset bilgilerini GitHub’dan okur ve AssetCollections listesini günceller.
        /// </summary>
        public static async Task ReadAssetCollectionsAsync(bool force = true)
        {
            if (IsFileReading)
            {
                Debug.Log("Data is already being read.");
                return;
            }

            if (!force && (AssetCollections = LoadAssetCollections()) is { Count: > 0 })
            {
                return;
            }

            AssetCollections = null;
            WorkerCount = 0;
            IsFileReading = true;
            string url = githubRepoURL + "Assets Info";
            Debug.Log("VIDA: Initiating data read.");

            List<VidaAssetCollection> collections = await ReadAssetInfoAsync(url, true);
            AssetCollections = collections;
            IsFileReading = false;
            await SaveAssetCollectionsAsync();
            Debug.Log("All data has been read.");
        }

        /// <summary>
        /// Starter.unitypackage dosyasını indirir ve Unity’ye import eder.
        /// </summary>
        /// <returns>İndirme başarılı ise true döner.</returns>
        public static async Task<bool> DownloadStarterAsync()
        {
            if (IsFileDownloading) return false;
            if (WorkerCount != 0)
            {
                Debug.Log("Worker is busy");
                return false;
            }
            WorkerCount = 1;
            IsFileDownloading = true;

            string url = githubRepoURL + "/Starter.unitypackage";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", authToken);
                request.SetRequestHeader("Accept", acceptToken);
                request.SendWebRequest();

                while (!request.isDone)
                    await Task.Delay(10);

                if (request.result == UnityWebRequest.Result.Success)
                {
                    JToken token = JToken.Parse(request.downloadHandler.text);
                    await ReadUnityPackageAsync(token);
                    Debug.Log("Starter package downloaded successfully!");
                    IsFileDownloading = false;
                    WorkerCount = 0;
                    return true;
                }
                else
                {
                    Debug.LogError("Failed to download package. Error: " + request.error);
                    IsFileDownloading = false;
                    WorkerCount = 0;
                    return false;
                }
            }
        }

        /// <summary>
        /// İlgili JToken içindeki unitypackage dosyasını indirir ve Unity’ye import eder.
        /// </summary>
        private static async Task ReadUnityPackageAsync(JToken token)
        {
            string packagePath = "Temp/TempPackage.unitypackage";

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(token["download_url"].ToString());
                response.EnsureSuccessStatusCode();
                byte[] content = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(packagePath, content);
            }

            AssetDatabase.ImportPackage(packagePath, true);
            AssetDatabase.Refresh();
            Debug.Log("Package imported successfully!");
        }

        /// <summary>
        /// Belirtilen itemName’e sahip koleksiyonu indirir.
        /// </summary>
        public static async Task DownloadItemAsync(string itemName)
        {
            VidaAssetCollection collection = AssetCollections?.Find(x => x.Name == itemName);
            if (collection == null)
            {
                Debug.LogError("Collection not found: " + itemName);
                return;
            }
            WorkerCount++;

            string url = githubRepoURL + collection.Location;
            string urlName = url.Split('/')[^1];
            url = url.Replace("/" + urlName, "");
            await DownloadItemAsync(url, collection.DownloadLocation, urlName);
            WorkerCount--;
        }

        /// <summary>
        /// Verilen URL’den, hedef klasöre (downloadLocation) dosyaları indirir.
        /// </summary>
        private static async Task DownloadItemAsync(string url, string downloadLocation, string targetName = "")
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.SetRequestHeader("Authorization", authToken);
                www.SetRequestHeader("Accept", acceptToken);
                www.SendWebRequest();

                while (!www.isDone)
                    await Task.Delay(100);

                if (www.result == UnityWebRequest.Result.Success)
                {
                    JToken files = JToken.Parse(www.downloadHandler.text);
                    foreach (var file in files)
                    {
                        string itemName = file["name"].ToString();
                        if (!string.IsNullOrEmpty(targetName) && itemName != targetName)
                            continue;

                        string itemType = file["type"].ToString();
                        if (itemType == "dir")
                        {
                            string subdirectoryUrl = $"{url}/{itemName}";
                            await DownloadItemAsync(subdirectoryUrl, Combine(downloadLocation, itemName));
                        }
                        else
                        {
                            if (itemName.Contains(".unitypackage"))
                            {
                                await ReadUnityPackageAsync(file);
                            }
                            else
                            {
                                await DownloadFileAsync(file, downloadLocation);
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError("DownloadItemAsync error: " + www.error);
                }
            }
        }

        /// <summary>
        /// Dosya indirme işlemini gerçekleştirir; dosya daha önce indirilmişse işlemi atlar.
        /// </summary>
        private static async Task DownloadFileAsync(JToken token, string downloadLocation)
        {
            string fileName = token["name"].ToString();
            string targetDir = Combine(dataPath, downloadLocation);

            if (!Exists(targetDir))
            {
                CreateDirectory(targetDir);
            }
            else
            {
                foreach (string file in Directory.GetFiles(targetDir))
                {
                    if (GetFileName(file) == fileName)
                    {
                        Debug.Log("File already downloaded: " + fileName);
                        return;
                    }
                }
            }

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(token["download_url"].ToString());
                response.EnsureSuccessStatusCode();
                string content = await response.Content.ReadAsStringAsync();
                string filePath = Combine(dataPath, downloadLocation, fileName);
                await File.WriteAllTextAsync(filePath, content);
                AssetDatabase.Refresh();
                Debug.Log("File downloaded: " + fileName);
            }
        }

        /// <summary>
        /// GitHub üzerindeki asset bilgilerini okur.
        /// </summary>
        /// <param name="url">GitHub API URL’si</param>
        /// <param name="isFirst">İlk çağrı mı?</param>
        /// <returns>Okunan koleksiyon listesi</returns>
        public static async Task<List<VidaAssetCollection>> ReadAssetInfoAsync(string url, bool isFirst = true)
        {
            WorkerCount++;
            List<VidaAssetCollection> loadedList = new List<VidaAssetCollection>();

            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.SetRequestHeader("Authorization", authToken);
                www.SendWebRequest();

                while (!www.isDone)
                    await Task.Delay(10);

                if (www.result == UnityWebRequest.Result.Success)
                {
                    JToken files = JToken.Parse(www.downloadHandler.text);
                    foreach (var file in files)
                    {
                        string fileName = file["name"].ToString();
                        if (fileName.EndsWith(".txt"))
                        {
                            VidaAssetCollection collection = await ReadTxtAsync(file);
                            if (collection != null)
                                loadedList.Add(collection);
                        }
                        else if (file["type"].ToString() == "dir")
                        {
                            List<VidaAssetCollection> subList = await ReadAssetInfoAsync(url + "/" + fileName, false);
                            loadedList.AddRange(subList);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("ReadAssetInfoAsync failed, retrying: " + www.error);
                    List<VidaAssetCollection> retryList = await ReadAssetInfoAsync(url, false);
                    loadedList.AddRange(retryList);
                }
            }

            WorkerCount--;
            if (isFirst)
            {
                while (WorkerCount > 0)
                {
                    await Task.Delay(10);
                }
            }
            return loadedList;
        }

        /// <summary>
        /// GitHub’dan indirilen metin dosyasını okur ve VidaAssetCollection nesnesine dönüştürür.
        /// </summary>
        private static async Task<VidaAssetCollection> ReadTxtAsync(JToken file)
        {
            WorkerCount++;
            VidaAssetCollection collection = new VidaAssetCollection();
            collection.Templates = new List<string>();

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(file["download_url"].ToString());
                response.EnsureSuccessStatusCode();
                string content = await response.Content.ReadAsStringAsync();
                string[] lines = content.Split(';');

                foreach (string item in lines)
                {
                    string line = item.Trim();
                    if (line.Length <= 1) continue;

                    int startIndex = line.IndexOf('{') + 1;
                    int endIndex = line.IndexOf('}');
                    if (startIndex <= 0 || endIndex <= startIndex)
                        continue;

                    string result = line.Substring(startIndex, endIndex - startIndex);
                    if (line.StartsWith("Template:"))
                    {
                        collection.Templates = new List<string>(result.Split(','));
                    }
                    else if (line.StartsWith("Info:"))
                    {
                        collection.Info = result;
                    }
                    else if (line.StartsWith("Location:"))
                    {
                        collection.Location = result;
                    }
                    else if (line.StartsWith("Menu:"))
                    {
                        collection.Menu = result;
                    }
                    else if (line.StartsWith("Name:"))
                    {
                        collection.Name = result;
                    }
                    else if (line.StartsWith("DownloadLocation:"))
                    {
                        collection.DownloadLocation = result;
                    }
                }
            }
            MainToolbar.ReloadNeeded = true;
            WorkerCount--;
            return collection;
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


}
