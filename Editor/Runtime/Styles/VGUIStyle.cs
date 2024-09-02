using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public static class VGUIStyle
    {
        public static readonly Color TextPrimary = new Color();
        public static readonly Color TextHeader = new Color();
        public static readonly Color TextOther = new Color();
        
        


        public static void ColorTester()
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.ColorField("Primary", TextPrimary);
            EditorGUILayout.ColorField("Header", TextHeader);
            EditorGUILayout.ColorField("Header", TextOther);
            
            
            EditorGUILayout.EndHorizontal();
        }
        
        
        // Centered gui style
        public static GUIStyle Center
        {
            get
            {
                if (_centeredGui == null)
                {
                    _centeredGui = new GUIStyle()
                    {
                        alignment = TextAnchor.MiddleCenter
                    };
                }

                return _centeredGui;
            }
        }


        private static GUIStyle _centeredGui;
        private static GUIStyle _background;
        
        
        public static readonly Color LightBorderColor = (Color) new Color32((byte) 90, (byte) 90, (byte) 90, byte.MaxValue);
        
        public static readonly Color LightGray = new Color(211f / 255f, 211f / 255f, 211f / 255f); // R: 211, G: 211, B: 211
        public static readonly Color LightBlue = new Color(173f / 255f, 216f / 255f, 230f / 255f); // R: 173, G: 216, B: 230
        public static readonly Color SoftGreen = new Color(152f / 255f, 251f / 255f, 152f / 255f); // R: 152, G: 251, B: 152
        public static readonly Color PastelYellow = new Color(255f / 255f, 255f / 255f, 153f / 255f); // R: 255, G: 255, B: 153
        public static readonly Color MatteGray = new Color(128f / 255f, 128f / 255f, 128f / 255f);
        public static readonly Color32 Background = new Color32(32, 32, 32, 122);
        public static readonly Color32 BackgroundSoft = new Color32(32, 32, 32, 65);

        public static readonly Color DefaultColor = new Color(1, 1, 1);
        
        public static GUIStyle CenteredLabel
        {
            get
            {
                GUIStyle guiStyle = new GUIStyle(GUI.skin.label);
                guiStyle.alignment = TextAnchor.MiddleCenter;
                return guiStyle;
            }
        }
        
        private static GUIStyle toolbarButton;
        private static GUIStyle toolbarButtonSelected;
        
        public static GUIStyle ToolbarButton
        {
            get
            {
                if (toolbarButton == null)
                    toolbarButton = new GUIStyle(EditorStyles.toolbarButton)
                    {
                        fixedHeight = 0.0f,
                        alignment = TextAnchor.MiddleCenter,
                        stretchHeight = true,
                        stretchWidth = false,
                        margin = new RectOffset(0, 0, 0, 0)
                    };
                return toolbarButton;
            }
        }

        
        public static void DrawRect(Rect rect, Color color, bool usePlaymodeTint = true)
        {
            if (Event.current.type != UnityEngine.EventType.Repaint) return;
            if (usePlaymodeTint)
            {
                EditorGUI.DrawRect(rect, color);
            }
            else
            {
                GUI.DrawTexture(rect, (Texture) EditorGUIUtility.whiteTexture);
            }
        }
        public static GUIStyle GetBoxStyle(Color32 color)
        {
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle( GUI.skin.window );
            }

            return _boxStyle;
        }
        private static GUIStyle _boxStyle;
        
        
        
        
        private static Texture2D MakeTex( int width, int height, Color col )
        {
            Color[] pix = new Color[width * height];
            for( int i = 0; i < pix.Length; ++i )
            {
                pix[ i ] = col;
            }
            Texture2D result = new Texture2D( width, height );
            result.SetPixels( pix );
            result.Apply();
            return result;
        }
    }
}