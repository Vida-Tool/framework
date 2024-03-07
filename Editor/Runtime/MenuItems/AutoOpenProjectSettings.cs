using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Vida.Framework
{

    [InitializeOnLoad]
    public class AutoOpenProjectSettings
    {
        static AutoOpenProjectSettings()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private static bool _opened = false;
        static void OnEditorUpdate()
        {
            // Check if the Build Settings window is open
            if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.titleContent.text.Contains("Build Settings"))
            {
                if(_opened) return;

                bool showVBuild = EditorPrefs.GetBool("OpenVBuildSettings", true);
                bool showSettings = EditorPrefs.GetBool("OpenProjectSettings", false);
                
                // Open the Project Settings window
                if(showSettings)
                    EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
                if(showVBuild)
                    EditorApplication.ExecuteMenuItem("Vida/VBuild Settings");
                
                _opened = true;
            }
            else
            {
                _opened = false;
            }
        }
    }
}
