using System;
using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Net.Http;
using static System.IO.Directory;
using static System.IO.Path;
using static UnityEngine.Application;

namespace VidaFramework.Editor
{
    public class VidaAssetCollection
    {
        public string Name { get; set; }
        public List<string> Templates { get; set; }
        public string Info { get; set; }
        public string Location { get; set; }
        public string Menu { get; set; }
        
        public string[] separatedMenu => Menu.Split('/');
    }
    
    public static class GithubConnector
    {
        public static List<VidaAssetCollection> AssetCollections = new List<VidaAssetCollection>();

        
        #region Private 
        private static readonly string githubRepoURL = "https://api.github.com/repos/nuribatuhansari/test/contents/";
        private static string authToken => $"Bearer {apiKey}";
        private static string acceptToken => "application/vnd.github.v3+json";
        
        private static readonly string downloadRoot = "DownloadedFiles";
        #endregion
        
        
                
        public static bool TryConnect()
        {
            UnityWebRequest www = UnityWebRequest.Get(githubRepoURL);
            www.SetRequestHeader("Authorization", authToken);
            www.SetRequestHeader("Accept", acceptToken);
            www.SendWebRequest();
            while(!www.isDone) {}
        
            if (www.isNetworkError || www.isHttpError)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        [MenuItem("Vida/Create dir")]
        public static void CreateDirs()
        {
            Dir();
            AssetDatabase.Refresh();
        }
        
        
        [MenuItem("Vida/ReadAssetInfo")]
        public static void ReadInfoFile(Action onCompleted = null)
        {
            AssetCollections = new List<VidaAssetCollection>();
            string url = githubRepoURL + $"Assets Info";
            ReadAssetInfo(url);
        }
        
        private static void ReadAssetInfo(string url)
        {
            UnityWebRequest www = UnityWebRequest.Get(url);
            www.SetRequestHeader("Authorization", authToken);
            www.SetRequestHeader("Accept", acceptToken);
            www.SendWebRequest();
            while(!www.isDone) {}

            if (www.result == UnityWebRequest.Result.Success)
            {
                JToken files  = JToken.Parse(www.downloadHandler.text);

                foreach (var file in files)
                {
                    string fileName = file["name"].ToString();
                    if (fileName.EndsWith(".txt"))
                    {
                        ReadTxt(file);
                    }
                    else if (file["type"].ToString() == "dir")
                    {
                        ReadAssetInfo(url + "/" + fileName);
                    }
                }
            }
        }

        private static async void ReadTxt(JToken file)
        {
            using (HttpClient client = new HttpClient())
            {
                // Dosyayı indir
                HttpResponseMessage response = await client.GetAsync(file["download_url"].ToString());
                response.EnsureSuccessStatusCode();

                // Dosyanın içeriğini oku
                string content = await response.Content.ReadAsStringAsync();
                string[] lines = content.Split(";");


                VidaAssetCollection collection = new VidaAssetCollection();

                foreach (string item in lines)
                {
                    string line = item.Trim();
                    
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
                }
                AssetCollections.Add(collection);
                MainToolbar.ReloadNeeded = true;
            }

        }
        
    
        static void PrintJson(JToken token, string path)
        {
            if (token.Type == JTokenType.Object)
            {
                foreach (JProperty prop in token.Children<JProperty>())
                {
                    string currentPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}/{prop.Name}";
                    
                    if (prop.Value.Type == JTokenType.Object)
                    {
                        PrintJson(prop.Value, currentPath);
                    }
                    else if (prop.Value.Type == JTokenType.String)
                    {
                        string value = prop.Value.ToString();
                        
                        AssetCollections.Add(new VidaAssetCollection()
                        {
                           // name = prop.Name,
                            //path = currentPath,
                            //templates = new List<string>(value.Split(','))
                        });
                        
                    }
                }
            }
        }
        
 
        private static string DownloadItem(string path,string dir,string itemName)
        {
            string newUrl = path;
            UnityWebRequest www = UnityWebRequest.Get(newUrl);
            www.SetRequestHeader("Authorization", authToken);
            www.SetRequestHeader("Accept", acceptToken);
            www.SendWebRequest();
        
            while(!www.isDone) {}
        
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError(www.error);
                return null;
            }
            else
            {
                string filePath = dataPath + dir + itemName;

                JToken token = JToken.Parse(www.downloadHandler.text);
                string base64Content = token["content"].ToString();
                byte[] data = Convert.FromBase64String(base64Content);
            
                System.IO.File.WriteAllBytes(filePath, data);
                AssetDatabase.Refresh();
                return filePath;
            }
        
        }

        
        
        
        private static void Dir(params string[] dir)
        {
            var fullPath = Combine(dataPath, downloadRoot);

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
}