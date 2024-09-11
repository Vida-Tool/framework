using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine.Networking;
using Vida.Framework.Editor;

namespace Vida.Framework.CodeEditor
{
    public class CodeData
    {
        public string category;
        public string header;
        public string data;
    }
    
    public static class DataReader
    {
        private static readonly string githubRepoURL = "https://api.github.com/repos/Vida-Tool/packages/contents/";
        private static string authToken => $"Bearer {apiKey}";
        public static string apiKey
        {
            get => EditorPrefs.GetString("GitApiKey","");
            set => EditorPrefs.SetString("GitApiKey", value);
        }
        
        public static List<CodeData> CodeData = new List<CodeData>();
        public static int TaskCount = 0;
        
        public static void LoadData()
        {
            if(TaskCount > 0) return;
            CodeData.Clear();
            LoadDataFromGithub(githubRepoURL  + $"Code Info","");
        }
        
        
        private static async void LoadDataFromGithub(string url,string category)
        {
            TaskCount++;
            UnityWebRequest www = UnityWebRequest.Get(url);
            www.SetRequestHeader("Authorization", authToken);
            www.SendWebRequest();
            while (!www.isDone)
            {
                await Task.Delay(10);
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("VIDA: Error in reading data.");
                TaskCount--;
                return;
            }
            
            JToken files  = JToken.Parse(www.downloadHandler.text);

            foreach (var file in files)
            {
                string fileName = file["name"].ToString();
                if (fileName.EndsWith(".txt"))
                {
                    ReadTxt(file, (d) =>
                    {
                        d.category = category;
                        CodeData.Add(d);
                    });
                }
                else if(fileName.EndsWith(".cs"))
                {
                    ReadTxt(file, (d) =>
                    {
                        d.category = category;
                        CodeData.Add(d);
                    });
                }
                else if (file["type"].ToString() == "dir")
                {
                    LoadDataFromGithub(url + "/" + fileName,fileName);
                }
            }
            
            TaskCount--;
        }
        
        private static async void ReadTxt(JToken file,Action<CodeData> callback)
        {
            TaskCount++;
            using HttpClient client = new HttpClient();
            
            // Dosyayı indir
            HttpResponseMessage response = await client.GetAsync(file["download_url"].ToString());
            response.EnsureSuccessStatusCode();
                
            // Dosyanın içeriğini oku
            string content = await response.Content.ReadAsStringAsync();
            

            // Header, '///' ıle baslayıp '///' olana kadar olan kısmı alır.
            int headerStartIndex = content.IndexOf("///", StringComparison.Ordinal) + 3;
            int headerEndIndex = content.IndexOf("///", headerStartIndex, StringComparison.Ordinal);
            
            if (headerStartIndex == -1 || headerEndIndex == -1)
            {
                Debug.LogError("Header not found in file.");
                return;
            }
            
            CodeData collection = new CodeData();
            collection.header = content.Substring(headerStartIndex, headerEndIndex - headerStartIndex).Trim();

            // remove header from content
            content = content.Remove(headerStartIndex-3, headerEndIndex + 3);
            
            // bos satiri kaldir
            content = content.Trim();
            
            collection.data = content;
            
            callback.Invoke(collection);
            TaskCount--;
        }
        
        /// <summary>
        /// Verilen dosya yolundaki .txt veya .cs dosyasını satır satır okur ve her satırı string olarak döndürür.
        /// </summary>
        /// <param name="filePath">Okunacak dosyanın tam yolu</param>
        /// <returns>Dosyanın satırlarını içeren string listesi</returns>
        public static List<string> ReadFileLines(string filePath)
        {
            // Dosya uzantısının .txt veya .cs olup olmadığını kontrol ediyoruz.
            if (!filePath.EndsWith(".txt") && !filePath.EndsWith(".cs"))
            {
                Debug.LogError("Sadece .txt ve .cs dosyalarını okuyabilirsiniz.");
                return null;
            }
    
            // Satırları tutmak için bir liste oluşturuyoruz.
            List<string> lines = new List<string>();
    
            try
            {
                // StreamReader kullanarak dosyayı açıp okuyoruz.
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    // Satır satır okuma
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines.Add(line); // Okunan her satırı listeye ekliyoruz.
                    }
                }
            }
            catch (IOException e)
            {
                Debug.LogError("Dosya okunamadı: " + e.Message);
            }
    
            return lines; // Satırları içeren listeyi döndürüyoruz.
        }
    }

}
