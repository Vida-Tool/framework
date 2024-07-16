using System;
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
            Debug.Log("xx");
        }

        [MenuItem("Vida/VBuild Settings")]
        public static void ShowWindow()
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
                var w = GetWindow<VBuildSettingsWindow>("VBuild Settings");
                w.bundleCode = PlayerSettings.Android.bundleVersionCode;
                w.version = PlayerSettings.bundleVersion;
                Debug.Log("Build Settings updated. Version: " + version + ", Bundle Code: " + bundleCode);
            }
            GUILayout.EndVertical();
        }
    }
}
