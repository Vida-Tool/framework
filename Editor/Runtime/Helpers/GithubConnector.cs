﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Net.Http;
using System.Threading.Tasks;
using Codice.Client.BaseCommands.Ls;
using Unity.Plastic.Newtonsoft.Json;
using static System.IO.Directory;
using static System.IO.Path;
using static UnityEngine.Application;

namespace Vida.Framework.Editor
{

    public static class GithubConnector
    {
        public static List<VidaAssetCollection> AssetCollections;

        
        #region Private 
        private static readonly string githubRepoURL = "https://api.github.com/repos/Vida-Tool/packages/contents/";
        private static string authToken => $"Bearer {apiKey}";
        private static string acceptToken => "application/vnd.github.v3+json";
        
        #endregion



        public static int WorkerCount { get; set; } = 0;
        public static bool IsFileReading { get; set; } = false;
        public static bool IsFileDownloading { get; set; } = false;
        

        

        public static async void SaveAssetCollections()
        {
            await Task.Delay(250);
            while (IsFileReading && AssetCollections is { Count: <= 0 })
            {
                await Task.Delay(10);
            }
            
            string json = JsonConvert.SerializeObject(AssetCollections);
            EditorPrefs.SetString("Collections", json);
        }

        public static List<VidaAssetCollection> LoadAssetCollections()
        {
            if (EditorPrefs.HasKey("Collections"))
            {
                string json = EditorPrefs.GetString("Collections",null);
                try
                {
                    return JsonConvert.DeserializeObject<List<VidaAssetCollection>>(json);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }
        
        
        
        public static void ResetConnection()
        {
            WorkerCount = 0;
            IsFileReading = false;
            IsFileDownloading = false;
            EditorPrefs.DeleteKey("Collections");
            AssetCollections = null;
        }


        private static bool _tasking;
        public static async Task TryConnect(Action<bool> result)
        {
            if(_tasking)
            {
                result = null;
                return;
            }
            _tasking = true;
            UnityWebRequest www = UnityWebRequest.Get(githubRepoURL);
            www.SetRequestHeader("Authorization", authToken);
            www.SetRequestHeader("Accept", acceptToken);
            www.SendWebRequest();
            while (!www.isDone)
            {
                await Task.Delay(10);
            }
            Debug.Log(www.result);
            if(www.result == UnityWebRequest.Result.Success)
            {
                result.Invoke(true);
            }
            else
            {
                result.Invoke(false);
            }
            _tasking = false;
        }


    
        public static void ReadInfoFile(bool force = true)
        {
            if(IsFileReading) return;
            
            if (WorkerCount.Equals(0) == false)
            {
                Debug.Log("Worker is busy");
                return;
            }

            if (force)
            {
                
            }
            else
            {
                AssetCollections = LoadAssetCollections();

                if (AssetCollections is { Count: > 0 })
                {
                    return;
                }
            }

            AssetCollections = null;
            WorkerCount = 0;
            IsFileReading = true;
            
            string url = githubRepoURL + $"Assets Info";
            Debug.Log("VIDA: Initiating data read.");
            ReadAssetInfo(url,true, (collections) =>
            {
                AssetCollections = collections;
                IsFileReading = false;
                SaveAssetCollections();
                Debug.Log("VIDA: All data has been read.");
            });
        }
        
        

        public static void DownloadStarter(Action<bool> callback = null,Action<DateTime> dateTime = null)
        {
            if(IsFileDownloading) return;
            
            if (WorkerCount.Equals(0) == false)
            {
                Debug.Log("Worker is busy");
                return;
            }
            WorkerCount = 1;
            IsFileDownloading = true;
            
            string url = githubRepoURL + "/Starter.unitypackage";
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", authToken);
            request.SetRequestHeader("Accept", acceptToken);
            
            request.SendWebRequest().completed += operation =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    JToken token = JToken.Parse(request.downloadHandler.text);
                    ReadUnityPackage(token);
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogError("Failed to download package. Error: " + request.error);
                    callback?.Invoke(false);
                    IsFileDownloading = false;
                    WorkerCount = 0;
                }
            };
        }

        private static async void ReadUnityPackage(JToken token)
        {
            string packagePath = "Temp/TempPackage.unitypackage";

            using (HttpClient client = new HttpClient())
            {
                // Dosyayı indir
                HttpResponseMessage response = await client.GetAsync(token["download_url"].ToString());
                response.EnsureSuccessStatusCode();

                // Dosyanın içeriğini oku
                var content = await response.Content.ReadAsByteArrayAsync();
                await System.IO.File.WriteAllBytesAsync(packagePath, content);
                
                AssetDatabase.ImportPackage(packagePath, true);
                
                // Dosyayı Unity'e import et
                AssetDatabase.Refresh();
                
                Debug.Log("File Downloaded!");
            }
                    

            Debug.Log("Package downloaded successfully!");
            IsFileDownloading = false;
            WorkerCount = 0;
        }
        
 

        public static async void DownloadItem(string itemName)
        {
            VidaAssetCollection collection = AssetCollections.Find(x => x.Name == itemName);
            if(collection == null)
            {
                Debug.LogError("Collection not found");
                return;
            }
            WorkerCount++;

            string url = githubRepoURL + $"{collection.Location}";
            string urlName = url.Split('/')[^1];
            url = url.Replace("/" + urlName, "");
            await DownloadItem(url, collection.DownloadLocation,urlName);
            
            WorkerCount--;
        }
        
        private static async Task DownloadItem(string url, string downloadLocation,string targetName = "")
        {
            UnityWebRequest www = UnityWebRequest.Get(url);
            www.SetRequestHeader("Authorization", authToken);
            www.SetRequestHeader("Accept", acceptToken);
            www.SendWebRequest();
            while (!www.isDone)
            {
                await Task.Delay(100);
            }

            if (www.result == UnityWebRequest.Result.Success)
            {
                JToken files = JToken.Parse(www.downloadHandler.text);
                foreach (var file in files)
                {
                    string itemName = file["name"].ToString();
                    if(targetName.Length > 0 && itemName != targetName) continue;
                    
                    string itemType = file["type"].ToString();
                    if (itemType == "dir")
                    {
                        // Bu bir klasördür, alt dizin içeriğini almak için aynı işlemi tekrarla
                        string subdirectoryUrl = $"{url}/{itemName}";
                        Task task = DownloadItem(subdirectoryUrl, downloadLocation+"/"+itemName,"");
                    }
                    else
                    {
                        if (itemName.Contains(".unitypackage"))
                        {
                            ReadUnityPackage(file);
                        }
                        else
                        {
                            // Bu bir dosyadır, dosyayı indirmek için mevcut işlemleri kullanabilirsiniz.
                            DownloadFile(file, downloadLocation);
                        }
                        
                    }
                }
            }
        }
        



        private static async void DownloadFile(JToken token,string downloadLocation)
        {
            // Dosya ismini al
            string fileName = token["name"].ToString();
            
            // Eğer indirmek için klasör yoksa oluştur
            if (Exists(Combine(dataPath, downloadLocation)) == false)
            {
                CreateDirectory(Combine(dataPath, downloadLocation));
            }
            else
            {
                // Eger dosya zaten indirilmis ise indirme
                string[] files = Directory.GetFiles(Combine(dataPath, downloadLocation));
                foreach (string file in files)
                {
                    var name = file.Split('/')[^1];
                    // name 2 is splitted by \ and / so we need to check both
                    var name2 = file.Split('\\')[^1];
                    
                    
                    if (name == fileName || name2 == fileName)
                    {
                        Debug.Log("File already downloaded");
                        return;
                    }
                }
            }
            
            using (HttpClient client = new HttpClient())
            {
                // Dosyayı indir
                HttpResponseMessage response = await client.GetAsync(token["download_url"].ToString());
                response.EnsureSuccessStatusCode();

                // Dosyanın içeriğini oku
                string content = await response.Content.ReadAsStringAsync();
                

                
                // Dosyayı oluştur
                string filePath = Combine(dataPath, downloadLocation, fileName);
                System.IO.File.WriteAllText(filePath, content);

                // Dosyayı Unity'e import et
                AssetDatabase.Refresh();
                
                Debug.Log("File Downloaded!");
            }
        }



        public static List<VidaAssetCollection> LoadedList;
        public static async void ReadAssetInfo(string url,bool isFirst,Action<List<VidaAssetCollection>> callback)
        {
            WorkerCount++;
            LoadedList = new List<VidaAssetCollection>();
            
            UnityWebRequest www = UnityWebRequest.Get(url);
            www.SetRequestHeader("Authorization", authToken);
            www.SendWebRequest();
            while (!www.isDone)
            {
                await Task.Delay(10);
            }

            
            if (www.result == UnityWebRequest.Result.Success)
            {
                JToken files  = JToken.Parse(www.downloadHandler.text);

                foreach (var file in files)
                {
                    string fileName = file["name"].ToString();
                    if (fileName.EndsWith(".txt"))
                    {
                        ReadTxt(file, (collection) =>
                        {
                            LoadedList.Add(collection);
                        });
                    }
                    else if (file["type"].ToString() == "dir")
                    {
                        ReadAssetInfo(url + "/" + fileName,false,(b) =>
                        {
                            LoadedList.AddRange(b);
                        });
                    }
                }
            }
            else
            {
                ReadAssetInfo(url, false,(b) =>
                {
                    LoadedList.AddRange(b);
                });
            }
            
            
            await Task.Delay(10);
            WorkerCount--;

            if (!isFirst)
            {
                return;
            }

   
            while (WorkerCount > 0)
            {
                await Task.Delay(10);
            }
            
            callback.Invoke(LoadedList);
        }

        private static async void ReadTxt(JToken file,Action<VidaAssetCollection> callback)
        {
            VidaAssetCollection collection = null;
            WorkerCount++;
            using (HttpClient client = new HttpClient())
            {
                // Dosyayı indir
                HttpResponseMessage response = await client.GetAsync(file["download_url"].ToString());
                response.EnsureSuccessStatusCode();
                
                // Dosyanın içeriğini oku
                string content = await response.Content.ReadAsStringAsync();
                string[] lines = content.Split(";");

                if (lines.Length > 1)
                {
                    collection = new VidaAssetCollection();
                    collection.Templates = new List<string>();
                }


                foreach (string item in lines)
                {
                    string line = item.Trim();
                    if(line.Length <= 1) continue;
                    int startIndex = line.IndexOf("{") + 1;
                    int endIndex = line.IndexOf("}");
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

                if (collection != null)
                {
                    callback.Invoke(collection);
                }
                
                MainToolbar.ReloadNeeded = true;
            }
            WorkerCount--;
        }
        
        

        
        private static void Dir(params string[] dir)
        {
            var fullPath = dataPath;

            foreach (var path in dir)
            {
                CreateDirectory(Combine(fullPath, path));
            }
        }
        
        public static string apiKey
        {
            get => EditorPrefs.GetString("GitApiKey","");
            set => EditorPrefs.SetString("GitApiKey", value);
        }
    }
    
    
    [System.Serializable]
    public class VidaAssetCollection
    {
        public string Name;
        public List<string> Templates { get; set; }
        public string Info { get; set; }
        public string Location { get; set; }
        public string DownloadLocation { get; set; }
        public string Menu { get; set; }
        
        
        public string[] separatedMenu => Menu.Split('/');

    }
}