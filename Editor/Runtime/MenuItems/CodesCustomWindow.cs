using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework
{
    public class CodesCustomWindow : EditorWindow
    {
        private static CodesWindow _codesWindow = new CodesWindow();
        
        [MenuItem("Vida/Codes",false,0)]
        private static void OpenWindow()
        {
            var window = GetWindow<CodesCustomWindow>();
            Rect rect = window.position;
            rect.width = 900;
            rect.height = 500;
            
            float x = Screen.currentResolution.width / 2f - rect.width / 2;
            float y = Screen.currentResolution.height / 2f - rect.height / 2;
            rect.x = x;
            rect.y = y;
            
            window.position = rect;
            window.minSize = new Vector2(900, 500);
            window.titleContent = new GUIContent("Codes","Framework codes");
        }
        
        private void OnGUI()
        {
            _codesWindow.Draw(new Vector2(position.width, position.height));
        }
    }
}
