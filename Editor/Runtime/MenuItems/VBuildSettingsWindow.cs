using UnityEditor;
using UnityEngine;

namespace Vida.Framework
{
    public class VBuildSettingsWindow : EditorWindow
    {
        private string version = "1.0";
        private int bundleCode = 1;
        private void OnDestroy()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        [MenuItem("Vida/VBuild Settings")]
        public static void ShowBuildSettingsWindow()
        {
            var w = GetWindow<VBuildSettingsWindow>("VBuild Settings");
            w.bundleCode = PlayerSettings.Android.bundleVersionCode;
            w.version = PlayerSettings.bundleVersion;
            w.maxSize = new Vector2(800, 400);
            EditorApplication.projectChanged += OnProjectChanged;
            
            float xCenter = Screen.currentResolution.width / 2 - w.position.width;
            float yCenter = Screen.currentResolution.height / 2 - w.position.height;
            // if position is outside of the screen, reset it
            if (w.position.x > Screen.currentResolution.width || w.position.y > Screen.currentResolution.height)
            {
                w.position = new Rect(xCenter, yCenter, 800, 400);
            }
            else if (w.position.x < 0 || w.position.y < 0)
            {
                w.position = new Rect(xCenter, yCenter, 800, 400);
            }

            var build = GetBuildPlayerWindow();
            if (build)
            {
                DockNextTo(w, build);
            }
        }
        
        private static void OnProjectChanged()
        {
            var w = GetWindow<VBuildSettingsWindow>("VBuild Settings");
            w.bundleCode = PlayerSettings.Android.bundleVersionCode;
            w.version = PlayerSettings.bundleVersion;
        }

        void OnGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            {
                // Version alanı
                GUILayout.BeginHorizontal();
                GUILayout.Label("Version:");
                version = GUILayout.TextField(version,GUILayout.Width(100));
                GUILayout.EndHorizontal();

                // Bundle code alanı
                GUILayout.BeginHorizontal();
                GUILayout.Label("Bundle Code:");
                bundleCode = EditorGUILayout.IntField(bundleCode);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndHorizontal();

            EditorPrefs.SetBool
                ("OpenVBuildSettings",GUILayout.Toggle(EditorPrefs.GetBool("OpenVBuildSettings", true), "Show VBuild"));
            EditorPrefs.SetBool
                ("OpenProjectSettings",GUILayout.Toggle(EditorPrefs.GetBool("OpenProjectSettings", false), "Show Settings"));

            

            // Apply button
            if (GUILayout.Button("Apply"))
            {
                PlayerSettings.bundleVersion = version;
                PlayerSettings.Android.bundleVersionCode = bundleCode;
                PlayerSettings.iOS.buildNumber = bundleCode.ToString();

                Debug.Log("Build Settings updated. Version: " + version + ", Bundle Code: " + bundleCode);
            }
            
            if (GUILayout.Button("Reload"))
            {
                this.bundleCode = PlayerSettings.Android.bundleVersionCode;
                this.version = PlayerSettings.bundleVersion;
                Debug.Log("Build Settings updated. Version: " + version + ", Bundle Code: " + bundleCode);
            }

            bool dock = EditorPrefs.GetBool("VidaDocking", true);
            // set color if dock is true
            GUI.backgroundColor = dock ? Color.green : Color.red;
            if (GUILayout.Button(dock ? "Docking : Enabled" : "Docking : Disabled"))
            {
                dock = !dock;
                EditorPrefs.SetBool("VidaDocking", dock);
                
                var build = GetBuildPlayerWindow();
                if (dock && build)
                {
                    DockNextTo(this, build);
                }
            }
            GUI.backgroundColor = Color.white;

            if (dock)
            {
                DockNextTo(this, GetBuildPlayerWindow());
            }
            
            GUILayout.EndVertical();
        }
        
        private static EditorWindow GetBuildPlayerWindow()
        {
            // Tüm açık EditorWindow'ları al
            var windows = Resources.FindObjectsOfTypeAll<BuildPlayerWindow>();

            // Her bir pencereyi kontrol et
            foreach (var window in windows)
            {
                if (window.GetType().Name == "BuildPlayerWindow")
                {
                    return window;
                }
            }

            // Eğer bulamazsak null döneriz
            return null;
        }
        
        private static void DockNextTo(EditorWindow main, EditorWindow docked)
        {
            if(docked == null) return; 
            // Get the position of the Build Settings window
            Rect dockedPos = docked.position;
            // Set the position of the VBuild Settings window next to the Build Settings window
            Rect mainTargetPos = new Rect(dockedPos.x + dockedPos.width, dockedPos.y, main.position.width, main.position.height);
            main.position = mainTargetPos;
        }
    }
}
