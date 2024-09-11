using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vida.Framework.CodeEditor;
using UnityEditor;

namespace Vida.Framework
{
    public static class CodeEditorDrawer
    {
        private static GUIStyle roundedBoxStyle;

        public static void Reset()
        {
            roundedBoxStyle = null;
        }
        
        private static void TryInit()
        {
            // Initialize rounded box style if not created yet
            if (roundedBoxStyle == null)
            {
                roundedBoxStyle = new GUIStyle(GUI.skin.box);
                roundedBoxStyle.normal.background = MakeTex(1, 1, new Color(0.15f, 0.15f, 0.15f)); // Dark background
                roundedBoxStyle.border = new RectOffset(12, 12, 12, 12); // Rounded corners
            }
        }

        public static void DrawCodeLine(CodeData data, float width)
        {
            TryInit();

            // Draw rounded background box
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(10); // Add padding

                GUILayout.BeginVertical(roundedBoxStyle, GUILayout.Width(width));
                {
                    DrawHighlightedCode(data.data);

                    var rect = GUILayoutUtility.GetLastRect();
                    // force to right on the screen
                    rect.x = width - 70;
                    rect.width = 50;
                    rect.height = 20;
                    if (GUI.Button(rect, "Copy"))
                    {
                        EditorGUIUtility.systemCopyBuffer = data.data;
                    }
                    GUILayout.Space(10); // Add padding
                }
                GUILayout.EndVertical();

                GUILayout.Space(10); // Add padding
            }
            GUILayout.EndHorizontal();
        }

        // Method to draw code with keyword and type highlighting
        private static void DrawHighlightedCode(string inputCode)
        {
            // Split input code into lines
            string[] lines = inputCode.Split('\n');

            // Draw each line
            for (int i = 0; i < lines.Length; i++)
            {
                int lineLength = lines[i].Length;
                int lineSpaceLength = lines[i].Select(c => c == ' ' ? 1 : 0).Sum();
                float guiWidth = lineLength * 1 - (lineSpaceLength * 5f) - 50;

                GUILayout.BeginHorizontal(GUILayout.Width(guiWidth));

                // Process each word in the line
                string[] words = lines[i].Split(' ');
                for (int j = 0; j < words.Length; j++)
                {
                    // Trim punctuation (e.g., semicolons, parentheses)
                    string word = words[j];

                    // Check if the word is a keyword or type
                    if (keywordColorMap.ContainsKey(word))
                    {
                        // Use the color assigned to the keyword or type
                        GUIStyle coloredStyle = new GUIStyle(EditorStyles.label);
                        coloredStyle.normal.textColor = keywordColorMap[word];
                        GUILayout.Label(word, coloredStyle);
                    }
                    else
                    {
                        // Default style for normal text
                        GUILayout.Label(word, EditorStyles.label);
                    }
                }

                GUILayout.EndHorizontal();
            }
        }

        // Helper method to convert hex color to Unity's Color
        private static Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                return color;
            }

            return Color.white;
        }


        // Visual Studio/Rider-like syntax coloring
        private static Dictionary<string, Color> keywordColorMap =
            new Dictionary<string, Color>
            {
                // C# Keywords
                { "static", HexToColor("#f71ba7") },
                { "public", HexToColor("#569CD6") },
                { "private", HexToColor("#569CD6") },
                { "protected", HexToColor("#569CD6") },
                { "class", HexToColor("#569CD6") },
                { "void", HexToColor("#569CD6") },
                { "if", HexToColor("#569CD6") },
                { "else", HexToColor("#569CD6") },
                { "for", HexToColor("#569CD6") },
                { "while", HexToColor("#569CD6") },
                { "using", HexToColor("#569CD6") },
                { "namespace", HexToColor("#569CD6") },
                { "return", HexToColor("#569CD6") },
                { "new", HexToColor("#569CD6") },
                { "switch", HexToColor("#569CD6") },
                { "case", HexToColor("#569CD6") },
                { "default", HexToColor("#569CD6") },
                { "try", HexToColor("#569CD6") },
                { "catch", HexToColor("#569CD6") },
                { "finally", HexToColor("#569CD6") },

                // C# Types
                { "int", HexToColor("#4EC9B0") },
                { "float", HexToColor("#4EC9B0") },
                { "double", HexToColor("#4EC9B0") },
                { "string", HexToColor("#4EC9B0") },
                { "bool", HexToColor("#4EC9B0") },
                { "char", HexToColor("#4EC9B0") },
                { "object", HexToColor("#4EC9B0") },

                // Strings
                { "\"", HexToColor("#D69D85") },

                // Comments (Single-line comments)
                { "//", HexToColor("#57A64A") },

                // Operators (we use generic color for now)
                { "=", HexToColor("#5232a8") },
                { "+", HexToColor("#5232a8") },
                { "-", HexToColor("#5232a8") },
                { "*", HexToColor("#5232a8") },
                { "/", HexToColor("#5232a8") },
                { "(", HexToColor("#5232a8") },
                { ")", HexToColor("#5232a8") },
                { "{", HexToColor("#f71ba7") },
                { "}", HexToColor("#f71ba7") },
            };

        // Helper method to create a solid color texture for GUI backgrounds
        private static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}