using UnityEditor;
using UnityEngine;

namespace Vida.Editor
{
    public static class VidaEditorGUI
    {
        public static void Title(string title, bool horizontalLine)
        {
            GUIStyle style1;
            style1 = new GUIStyle(EditorStyles.largeLabel);
            style1.alignment = TextAnchor.MiddleCenter;
            style1.fontStyle = FontStyle.Bold;
            
            // VidaGUILayoutOptions.ExpandWidth(true)
            Rect rect = GUILayoutUtility.GetRect(0.0f,18f,style1,VidaGUILayoutOptions.ExpandWidth(true));
            GUI.Label(rect, title, style1);
            if (!horizontalLine)
                return;
            
            GUILayout.Space(3f);
            DrawSolidRect(rect.AlignBottom(1f), VidaGUIStyles.LightBorderColor);
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