using System;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public class SettingsWindow
    {
        private VGuiStyleSO Style => VGuiStyleSO.Style;

        public void Draw()
        {
            
            GUILayout.BeginVertical();
            GUILayout.EndVertical();
            
        }
    }
}