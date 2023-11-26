using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace VIDA.Editor
{
    public class UnityInsidePackages
    {
        public static string[] packageNames = new []
        {
            "Cinemachine", "URP", "Recorder","Mathematics"
        };
        private static string[] installablePackages = new []
        {
            "com.unity.cinemachine", "com.unity.render-pipelines.universal", "com.unity.recorder","com.unity.mathematics"
        };
                    
        [MenuItem("Vida/Package Dependencies Installer")]
        public static void Install()
        {
            ListRequest listRequest = Client.List();
            while (!listRequest.IsCompleted)
            {
                if (listRequest.Status == StatusCode.Failure)
                {
                    Debug.LogError("PackageManager ListRequest failed.");
                    return;
                }
            }
                
            for (int i = 0,length = installablePackages.Length; i < length; i++)
            {
                if (GetPrefs(packageNames[i], false))
                {
                    SetPrefs(packageNames[i], false);
                    Install(listRequest,installablePackages[i]);
                }
            }
        }
                
        private static void Install(ListRequest listRequest,string packageName)
        {
            foreach (var package in listRequest.Result)
            {
                if (package.name == packageName)
                {
                    Debug.Log($"{packageName} is already installed and skipped.");
                    return;
                }
            }
                        
            AddRequest addRequest = Client.Add(packageName);
            while (!addRequest.IsCompleted)
            {
                if (addRequest.Status == StatusCode.Failure)
                {
                    Debug.LogError($"{packageName} package installation failed. Try install manually.");
                    return;
                }
            }
                        
            Debug.Log($"{packageName} is installed.");
        }
        
        
        private static bool GetPrefs(string v,bool d = false) => EditorPrefs.GetBool(v, d);
        private static void SetPrefs(string v,bool value) => EditorPrefs.SetBool(v, value);
    }
}