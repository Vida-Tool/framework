
namespace Vida.Framework
{
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    public class BuildSettingsWatcher
    {
        private static bool buildSettingsOpen = false;

        static BuildSettingsWatcher()
        {
            EditorApplication.update -= CheckBuildSettingsWindow;
            EditorApplication.update += CheckBuildSettingsWindow;
        }

        private static EditorWindow GetActiveWindow(string windowName)
        {
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();

            foreach (var window in windows)
            {
                if (window.GetType().Name == windowName)
                {
                    return window;
                }
            }

            return null;
        }
        
        private static void CheckBuildSettingsWindow()
        {
            bool isBuildSettingsOpen = GetActiveWindow("BuildPlayerWindow") != null;

            // Eğer Build Settings penceresi açıldıysa ve daha önce açılmamışsa
            if (isBuildSettingsOpen && !buildSettingsOpen)
            {
                buildSettingsOpen = true;
                OnBuildSettingsOpened();
            }
            // Eğer Build Settings penceresi kapandıysa ve daha önce açıksa
            else if (!isBuildSettingsOpen && buildSettingsOpen)
            {
                buildSettingsOpen = false;
                OnBuildSettingsClosed();
            }


        }

        private static void OnBuildSettingsOpened()
        {
            // Burada kendi kodunuzu çalıştırabilirsiniz
            bool open = EditorPrefs.GetBool("OpenVBuildSettings", true);

            if (open)
            {
                var w = GetActiveWindow("VBuildSettingsWindow");
                if (w == null)
                {
                    VBuildSettingsWindow.ShowBuildSettingsWindow();
                }
            }
        }

        private static void OnBuildSettingsClosed()
        {
            // Burada kendi kodunuzu çalıştırabilirsiniz
            var w = GetActiveWindow("VBuildSettingsWindow");
            if (w != null)
            {
                var window = w as VBuildSettingsWindow;
                window.BuildSettingsClosed();
            }
        }
    }
}
