using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Vida.Editor
{
    public static class VidaEditorGUI
    {
        public static int DefaultFontSize = 12;
        

        
        public static void Title(string title, bool horizontalLine,TextAnchor anchor = TextAnchor.MiddleCenter,int fontSize = 14)
        {
            GUIStyle style1;
            style1 = new GUIStyle(EditorStyles.largeLabel);
            style1.alignment = anchor;
            style1.fontStyle = FontStyle.Bold;
            style1.fontSize = fontSize;
            
            // VidaGUILayoutOptions.ExpandWidth(true)
            Rect rect = GUILayoutUtility.GetRect(0.0f,18f,style1,VidaGUILayoutOptions.ExpandWidth(true));
            GUI.Label(rect, title, style1);
            if (!horizontalLine)
                return;
            
            GUILayout.Space(3f);
            DrawSolidRect(rect.AlignBottom(1f), VidaGUIStyles.LightBorderColor);
            GUILayout.Space(5f);
        }
        
        
        
        public static void DrawSolidRect(Rect rect, Color color, bool usePlaymodeTint = true)
        {
            if (Event.current.type != UnityEngine.EventType.Repaint)
                return;
            if (usePlaymodeTint)
            {
                EditorGUI.DrawRect(rect, color);
            }
            else
            {
                GUI.DrawTexture(rect, (Texture) EditorGUIUtility.whiteTexture);
            }
        }
    }
}