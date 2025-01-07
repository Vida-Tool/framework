using System;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public class GitLoginWindow
    {
        private VGuiStyleSO Style => VGuiStyleSO.Style;

        public void Draw()
        {

            GUILayout.BeginVertical();
            {
                ScriptableObjectTester();
            }
            GUILayout.EndVertical();

        }


        private void ScriptableObjectTester()
        {
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                GUILayout.Space(10);
                GUIStyle background1 = VCustomGUI.GetBoxStyle(Style.Background);
                GUILayout.BeginVertical(background1);
                {
                    "Text".VTitle(true);
                    DrawLabelBox("TextPrimary", Style.TextPrimary, Style.TextPrimary, (c) => Style.TextPrimary = c);
                    DrawLabelBox("TextHeader", Style.TextHeader, Style.TextHeader, (c) => Style.TextHeader = c);
                    DrawLabelBox("TextOther", Style.TextOther, Style.TextOther, (c) => Style.TextOther = c);

                }
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                GUIStyle background2 = VCustomGUI.GetBoxStyle(Style.Background);
                GUILayout.BeginVertical(background2);
                {
                    "Background".VTitle(true);
                    DrawLabelBox("Background", Style.Background, Style.TextOther, (c) => Style.Background = c);
                    DrawLabelBox("Popup", Style.Popup, Style.TextOther, (c) => Style.Popup = c);
                    DrawLabelBox("LightBorderColor", Style.LightBorderColor, Style.TextOther,
                        (c) => Style.LightBorderColor = c);
                    DrawLabelBox("DarkBorderColor", Style.DarkBorderColor, Style.TextOther,
                        (c) => Style.DarkBorderColor = c);
                }
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
        }

        private void DrawLabelBox(string label, Color color, Color labelColor, Action<Color> colorGetter)
        {
            GUILayout.BeginHorizontal();
            {
                label.VLabel(labelColor);
                GUILayout.FlexibleSpace();
                var col = color;
                color = EditorGUILayout.ColorField(color);

                if (col != color)
                {
                    colorGetter.Invoke(color);

                    // set dirty
                    EditorUtility.SetDirty(Style);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(3);
        }
    }
}