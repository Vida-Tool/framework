using UnityEditor;
using UnityEngine;
using Vida.Framework.Editor;

namespace Vida.Framework
{
    public static class VCustomGUI
    {
        private static VGuiStyleSO Style => VGuiStyleSO.Style;

        #region Text
        public static void VLabel(this string text, Color color)
        {
            GUILayout.Label(text, AddColor(DefaultLabelStyle, color));
        }
        public static void VHeader(this string text, Color color)
        {
            GUILayout.Label(text, AddColor(HeaderLabelStyle, color));
        }
        public static void VHeader(this string text)
        {
            GUILayout.Label(text, AddColor(HeaderLabelStyle, Style.TextHeader));
        }
        public static void VTitle(this string text, bool horizontalLine,TextAnchor anchor = TextAnchor.MiddleCenter,int fontSize = 14,RectOffset padding = null)
        {
            Title(text, horizontalLine,Style.TextPrimary,anchor,fontSize,padding);
        }
        public static void VTitle(this string text, bool horizontalLine,Color color,TextAnchor anchor = TextAnchor.MiddleCenter,int fontSize = 14,RectOffset padding = null)
        {
            Title(text, horizontalLine,color,anchor,fontSize,padding);
        }
        public static void Title(string title, bool horizontalLine,Color color,TextAnchor anchor = TextAnchor.MiddleCenter,int fontSize = 14,RectOffset padding = null)
        {
            if(padding == null) padding = new RectOffset(0,0,0,0);
            GUIStyle style1;
            style1 = new GUIStyle(TitleStyle);
            style1.alignment = anchor;
            style1.fontStyle = FontStyle.Bold;
            style1.fontSize = fontSize;
            style1.normal.textColor = color;

            style1.padding = padding;
            // VidaGUILayoutOptions.ExpandWidth(true)
            Rect rect = GUILayoutUtility.GetRect(0.0f,fontSize*2,style1,VidaGUILayoutOptions.ExpandWidth(true));
            GUI.Label(rect, title, style1);
            
            if (!horizontalLine)
                return;
            
            DrawSolidRect(rect.AlignBottom(1f), Style.LightBorderColor);
            GUILayout.Space(5);
        }
        private static GUIStyle AddColor(this GUIStyle style, Color color)
        {
            if(style == null) style = new GUIStyle();
            style.normal.textColor = color;
            return style;
        } 

        
        private static GUIStyle _defaultLabelStyle;
        private static GUIStyle _headerLabelStyle;
        private static GUIStyle _titleStyle;

        public static GUIStyle DefaultLabelStyle
        {
            get
            {
                if (_defaultLabelStyle == null)
                {
                    _defaultLabelStyle = new GUIStyle(GUI.skin.label)
                    {
                        normal = {textColor = Style.TextPrimary}
                    };
                }

                return _defaultLabelStyle;
            }
        }
        
        public static GUIStyle HeaderLabelStyle
        {
            get
            {
                if (_headerLabelStyle == null)
                {
                    _headerLabelStyle = new GUIStyle(GUI.skin.label)
                    {
                        normal = {textColor = Style.TextHeader},
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 12
                    };
                }

                return _headerLabelStyle;
            }
        }
        
        public static GUIStyle TitleStyle
        {
            get
            {
                if (_titleStyle == null)
                {
                    _titleStyle = new GUIStyle(GUI.skin.label)
                    {
                        normal = {textColor = Style.TextPrimary},
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                        fontSize = 14
                    };
                }

                return _titleStyle;
            }
        }
        #endregion
        
        #region Background
        
        public static void DrawSolidRect(Rect rect, Color color, bool usePlaymodeTint = true)
        {
            if (Event.current.type != UnityEngine.EventType.Repaint) return;
            
            rect.position -= new Vector2(2,0);
            rect.width += 4;
            rect.height += 1;
            
            if (usePlaymodeTint)
            {
                EditorGUI.DrawRect(rect, color);
            }
            else
            {
                GUI.DrawTexture(rect, (Texture) EditorGUIUtility.whiteTexture);
            }
        }
        

        public static GUIStyle GetBoxStyle(Color color)
        {
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle( GUI.skin.window);
            }
            _boxStyle.normal.background = MakeTex(64, 64, color,Style.DarkBorderColor,1,0);
            _boxStyle.padding = new RectOffset(5, 5, 0, 5);
            return _boxStyle;
        }
        private static GUIStyle _boxStyle;
        
        public static GUIStyle GetDefaultBoxStyle(Color color)
        {
            if (_defaultBoxStyle == null)
            {
                _defaultBoxStyle = new GUIStyle( GUI.skin.window );
            }
            return _defaultBoxStyle;
        }
        private static GUIStyle _defaultBoxStyle;
        
        
        private static Texture2D MakeTex(int width, int height, Color mainColor, Color borderColor, int borderThickness, int cornerRadius)
        {
            Texture2D texture = new Texture2D(width, height);
            Color[] colors = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Determine if the pixel is in the border area
                    bool isBorder = x < borderThickness || x >= width - borderThickness || y < borderThickness || y >= height - borderThickness;

                    bool isCorner = 
                        (x < cornerRadius && y < cornerRadius && (cornerRadius - x) * (cornerRadius - x) + (cornerRadius - y) * (cornerRadius - y) > cornerRadius * cornerRadius) ||
                        (x < cornerRadius && y >= height - cornerRadius && (cornerRadius - x) * (cornerRadius - x) + (cornerRadius - (height - y - 1)) * (cornerRadius - (height - y - 1)) > cornerRadius * cornerRadius) ||
                        (x >= width - cornerRadius && y < cornerRadius && (cornerRadius - (width - x - 1)) * (cornerRadius - (width - x - 1)) + (cornerRadius - y) * (cornerRadius - y) > cornerRadius * cornerRadius) ||
                        (x >= width - cornerRadius && y >= height - cornerRadius && (cornerRadius - (width - x - 1)) * (cornerRadius - (width - x - 1)) + (cornerRadius - (height - y - 1)) * (cornerRadius - (height - y - 1)) > cornerRadius * cornerRadius);

   
                    // Set the color for this pixel


                    if (isBorder || isCorner)
                    {
                        colors[y * width + x] = borderColor;
                    }
                    else
                    {
                        colors[y * width + x] = mainColor;
                    }

                }
            }

            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }

        
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
        
        
        #endregion



        
        


    }
}