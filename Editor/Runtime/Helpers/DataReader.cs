using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Vida.Framework.CodeEditor
{
    public class CodeData
    {
        public string category;
        public string header;
        public string data;
        public string fileName;
    }
    
    public static class DataReader
    {
        private const string CodeRootDirectory = "Code Info";
        private const string DefaultCategoryName = "General";
        private const string TextExtension = ".txt";
        private const string CSharpExtension = ".cs";
        private const string GitHubAcceptHeader = "application/vnd.github.v3+json";
        private const string GitHubRawAcceptHeader = "application/vnd.github.raw+json";

        private static readonly string githubRepoURL = "https://api.github.com/repos/Vida-Tool/packages/contents/";
        private static string authToken => $"Bearer {apiKey}";
        public static string apiKey
        {
            get => EditorPrefs.GetString("GitApiKey","");
            set => EditorPrefs.SetString("GitApiKey", value);
        }
        
        public static List<CodeData> CodeData = new List<CodeData>();
        public static int TaskCount = 0;
        public static int DataVersion { get; private set; }
        public static bool HasLoaded { get; private set; }
        
        public static async void LoadData()
        {
            if(TaskCount > 0) return;

            TaskCount = 1;
            HasLoaded = false;
            CodeData.Clear();
            DataVersion++;

            try
            {
                List<CodeData> codeDatas = new List<CodeData>();
                await LoadDataFromGithub(GetCodeRootUrl(), "", "", codeDatas);
                codeDatas.Sort(CompareCodeData);
                CodeData.Clear();
                CodeData.AddRange(codeDatas);
            }
            catch (Exception e)
            {
                Debug.LogWarning("VIDA: Code data okunamadı. " + e.Message);
            }
            finally
            {
                TaskCount = 0;
                HasLoaded = true;
                DataVersion++;
            }
        }
        
        
        private static async Task LoadDataFromGithub(string url, string category, string relativePath, List<CodeData> codeDatas)
        {
            string response = await SendGithubRequest(url, GitHubAcceptHeader);
            if (string.IsNullOrEmpty(response))
            {
                return;
            }

            JToken files = JToken.Parse(response);
            if (files.Type != JTokenType.Array)
            {
                return;
            }

            List<JToken> sortedFiles = new List<JToken>();
            foreach (JToken file in files)
            {
                sortedFiles.Add(file);
            }

            sortedFiles.Sort(CompareGithubFile);

            foreach (JToken file in sortedFiles)
            {
                string fileName = file["name"]?.ToString();
                string fileType = file["type"]?.ToString();
                if (string.IsNullOrEmpty(fileName))
                {
                    continue;
                }

                if (fileType == "dir")
                {
                    string nextCategory = string.IsNullOrEmpty(category) ? fileName : category;
                    string nextRelativePath = GetRelativePath(relativePath, fileName);
                    string nextUrl = file["url"]?.ToString();
                    if (!string.IsNullOrEmpty(nextUrl))
                    {
                        await LoadDataFromGithub(nextUrl, nextCategory, nextRelativePath, codeDatas);
                    }

                    continue;
                }

                if (IsCodeFile(fileName))
                {
                    await ReadCodeFile(file, category, relativePath, codeDatas);
                }
            }
        }
        
        private static async Task ReadCodeFile(JToken file, string category, string relativePath, List<CodeData> codeDatas)
        {
            string fileName = file["name"]?.ToString();
            string content = await ReadFileContent(file);
            if (string.IsNullOrEmpty(fileName) || content == null)
            {
                return;
            }
            
            CodeData collection = new CodeData();
            collection.category = GetCategoryName(category);
            collection.fileName = fileName;

            if (TryReadLegacyHeaderBlock(content, out string legacyHeader, out string codeContent))
            {
                collection.header = legacyHeader;
                collection.data = codeContent.Trim();
            }
            else
            {
                collection.header = GetCodeHeader(fileName, relativePath);
                collection.data = NormalizeLineEndings(content).Trim();
            }

            codeDatas.Add(collection);
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

        private static async Task<string> ReadFileContent(JToken file)
        {
            string apiUrl = file["url"]?.ToString();
            if (!string.IsNullOrEmpty(apiUrl))
            {
                string content = await SendGithubRequest(apiUrl, GitHubRawAcceptHeader);
                if (content != null)
                {
                    return content;
                }
            }

            string downloadUrl = file["download_url"]?.ToString();
            if (!string.IsNullOrEmpty(downloadUrl))
            {
                return await SendGithubRequest(downloadUrl, GitHubRawAcceptHeader);
            }

            return null;
        }

        private static async Task<string> SendGithubRequest(string url, string acceptHeader)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                SetGithubHeaders(www, acceptHeader);
                www.SendWebRequest();
                while (!www.isDone)
                {
                    await Task.Delay(10);
                }

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("VIDA: GitHub verisi okunamadı. " + www.error + " | " + url);
                    return null;
                }

                return www.downloadHandler.text;
            }
        }

        private static void SetGithubHeaders(UnityWebRequest www, string acceptHeader)
        {
            if (!string.IsNullOrEmpty(apiKey))
            {
                www.SetRequestHeader("Authorization", authToken);
            }

            www.SetRequestHeader("Accept", acceptHeader);
            www.SetRequestHeader("User-Agent", "VidaFramework");
        }

        private static bool TryReadLegacyHeaderBlock(string content, out string header, out string codeContent)
        {
            header = null;
            codeContent = content;

            string normalizedContent = NormalizeLineEndings(content).TrimStart('\uFEFF');
            string[] lines = normalizedContent.Split('\n');
            if (lines.Length < 3 || lines[0].Trim() != "///")
            {
                return false;
            }

            int headerEndIndex = -1;
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Trim() == "///")
                {
                    headerEndIndex = i;
                    break;
                }
            }

            if (headerEndIndex <= 1)
            {
                return false;
            }

            header = string.Join("\n", lines, 1, headerEndIndex - 1).Trim();
            codeContent = string.Join("\n", lines, headerEndIndex + 1, lines.Length - headerEndIndex - 1);
            return !string.IsNullOrEmpty(header);
        }

        private static bool IsCodeFile(string fileName)
        {
            return fileName.EndsWith(TextExtension, StringComparison.OrdinalIgnoreCase) ||
                   fileName.EndsWith(CSharpExtension, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetCodeRootUrl()
        {
            return githubRepoURL + Uri.EscapeDataString(CodeRootDirectory);
        }

        private static string GetRelativePath(string relativePath, string fileName)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return fileName;
            }

            return relativePath + "/" + fileName;
        }

        private static string GetCategoryName(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return DefaultCategoryName;
            }

            return GetDisplayName(category);
        }

        private static string GetCodeHeader(string fileName, string relativePath)
        {
            string title = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrEmpty(relativePath))
            {
                return GetDisplayName(title);
            }

            int slashIndex = relativePath.IndexOf("/", StringComparison.Ordinal);
            if (slashIndex < 0 || slashIndex >= relativePath.Length - 1)
            {
                return GetDisplayName(title);
            }

            return GetDisplayName(relativePath.Substring(slashIndex + 1) + "/" + title);
        }

        private static string GetDisplayName(string value)
        {
            string normalizedValue = Uri.UnescapeDataString(value).Replace("\\", "/");
            string[] parts = normalizedValue.Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = ObjectNames.NicifyVariableName(parts[i].Replace("-", " ").Replace("_", " "));
            }

            return string.Join(" / ", parts);
        }

        private static string NormalizeLineEndings(string content)
        {
            return content.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        private static int CompareGithubFile(JToken a, JToken b)
        {
            string aType = a["type"]?.ToString();
            string bType = b["type"]?.ToString();
            bool aIsDir = aType == "dir";
            bool bIsDir = bType == "dir";
            if (aIsDir != bIsDir)
            {
                return aIsDir ? -1 : 1;
            }

            string aName = a["name"]?.ToString();
            string bName = b["name"]?.ToString();
            return string.Compare(aName, bName, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareCodeData(CodeData a, CodeData b)
        {
            int categoryCompare = string.Compare(a.category, b.category, StringComparison.OrdinalIgnoreCase);
            if (categoryCompare != 0)
            {
                return categoryCompare;
            }

            return string.Compare(a.header, b.header, StringComparison.OrdinalIgnoreCase);
        }
    }

}
