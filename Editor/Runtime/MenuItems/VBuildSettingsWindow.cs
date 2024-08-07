using System.Collections.Generic;
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
            EditorApplication.update -= OnProjectChanged;
        }

        public void BuildSettingsClosed()
        {
            Close();
        }

        public void BuildSettingsUpdate()
        {
            bundleCode = PlayerSettings.Android.bundleVersionCode;
            version = PlayerSettings.bundleVersion;
            
            bool dock = EditorPrefs.GetBool("VidaDocking", true);

            if (dock)
            {
                DockNextTo(this, GetActiveWindow("BuildPlayerWindow"));
            }
        }

        [MenuItem("Vida/VBuild Settings")]
        public static void ShowBuildSettingsWindow()
        {
            EditorApplication.update += OnProjectChanged;

            var w = GetWindow<VBuildSettingsWindow>("VBuild Settings");
            w.bundleCode = PlayerSettings.Android.bundleVersionCode;
            w.version = PlayerSettings.bundleVersion;
            w.maxSize = new Vector2(800, 400);
            

            
            float xCenter = Screen.currentResolution.width / 2f - w.position.width;
            float yCenter = Screen.currentResolution.height / 2f - w.position.height;
            // if position is outside of the screen, reset it
            if (w.position.x > Screen.currentResolution.width || w.position.y > Screen.currentResolution.height)
            {
                w.position = new Rect(xCenter, yCenter, 800, 400);
            }
            else if (w.position.x < 0 || w.position.y < 0)
            {
                w.position = new Rect(xCenter, yCenter, 800, 400);
            }

            var build = GetActiveWindow("BuildPlayerWindow");
            if (build)
            {
                DockNextTo(w, build);
            }
        }
        
        
        
        private static void OnProjectChanged()
        {
            var w = GetActiveWindow("VBuildSettingsWindow");
            if (w)
            {
                var b = w as VBuildSettingsWindow;
                b.bundleCode = PlayerSettings.Android.bundleVersionCode;
                b.version = PlayerSettings.bundleVersion;
                
                bool dock = EditorPrefs.GetBool("VidaDocking", true);

                // only if repaint
                
                
                if (dock)
                {
                    DockNextTo(b, GetActiveWindow("BuildPlayerWindow"));
                }
            }
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
                
                var build = GetActiveWindow("BuildPlayerWindow");
                if (dock && build)
                {
                    DockNextTo(this, build);
                }
            }
            GUI.backgroundColor = Color.white;

            if (dock)
            {
                DockNextTo(this, GetActiveWindow("BuildPlayerWindow"));
            }
            
            GUILayout.EndVertical();
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

        private static List<Vector3> positions = new List<Vector3>();
        private static void DockNextTo(EditorWindow main, EditorWindow docked)
        {
            if(docked == null) return; 
            // Get the position of the Build Settings window
            Rect dockedPos = docked.position;
            positions.Add(new Vector3(dockedPos.x, dockedPos.y, 0));
            if(positions.Count >= 5) positions.RemoveAt(0);
            
            // if all positions not same return
            if (positions.Count > 1)
            {
                for (int i = 1; i < positions.Count; i++)
                {
                    if (positions[i] != positions[i - 1])
                    {
                        return;
                    }
                }
            }
            
            // Set the position of the VBuild Settings window next to the Build Settings window
            Rect mainTargetPos = new Rect(dockedPos.x + dockedPos.width, dockedPos.y, main.position.width, main.position.height);
            
            main.position = mainTargetPos;
        }
    }
}
