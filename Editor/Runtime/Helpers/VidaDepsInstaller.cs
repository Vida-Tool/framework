// Assets/Editor/VidaDepsInstaller.cs
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

public static class VidaDepsInstaller
{
    const string DepId  = "com.coffee.ui-effect";
    const string DepUrl = "https://github.com/mob-sakai/UIEffect.git?path=Packages";

    [InitializeOnLoadMethod]
    static void AutoInstallOnce()
    {
        // Dilersen bu satırı yoruma alıp yalnız menüden çalıştırırsın
        EnsurePackage(DepId, DepUrl);
    }

    [MenuItem("Vida/Install Dependencies")]
    static void InstallFromMenu()
    {
        EnsurePackage(DepId, DepUrl);
    }

    static void EnsurePackage(string id, string url)
    {
        var list = Client.List(true);
        EditorApplication.update += PollList;

        void PollList()
        {
            if (!list.IsCompleted) return;
            EditorApplication.update -= PollList;

            bool has = false;
            if (list.Status == StatusCode.Success)
            {
                foreach (var p in list.Result)
                {
                    if (p.name == id) { has = true; break; }
                }
            }
            if (has)
            {
                UnityEngine.Debug.Log($"Package already present: {id}");
                return;
            }

            var add = Client.Add(url);
            EditorApplication.update += PollAdd;

            void PollAdd()
            {
                if (!add.IsCompleted) return;
                EditorApplication.update -= PollAdd;

                if (add.Status == StatusCode.Success)
                    UnityEngine.Debug.Log($"Installed: {add.Result.name} {add.Result.version}");
                else
                    UnityEngine.Debug.LogError($"Package add failed: {add.Error.message}");
            }
        }
    }
}