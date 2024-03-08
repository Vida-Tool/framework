using UnityEditor;
using UnityEngine;

namespace Vida.Framework
{
    public class VBuildSettingsWindow : EditorWindow
    {
        private string version = "1.0";
        private int bundleCode = 1;

        [MenuItem("Vida/VBuild Settings")]
        public static void ShowWindow()
        {
            var w = GetWindow<VBuildSettingsWindow>("VBuild Settings");
            w.bundleCode = PlayerSettings.Android.bundleVersionCode;
            w.version = PlayerSettings.bundleVersion;
            w.maxSize = new Vector2(400, 50);
        }

        void OnGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            {
                // Version alanı
                GUILayout.BeginHorizontal();
                GUILayout.Label("Version:");
                version = GUILayout.TextField(version);
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
            GUILayout.EndVertical();

            if (PlayerSettings.bundleVersion != version)
            {
                PlayerSettings.bundleVersion = version;
            }
            if (PlayerSettings.Android.bundleVersionCode != bundleCode)
            {
                PlayerSettings.Android.bundleVersionCode = bundleCode;
            }
            if (PlayerSettings.iOS.buildNumber != bundleCode.ToString())
            {
                PlayerSettings.iOS.buildNumber = bundleCode.ToString();
            }
            
        }
    }
}
