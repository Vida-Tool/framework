using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Vida.Framework.CodeEditor;
using Vida.Framework.Editor;

namespace Vida.Framework
{
    public class CodesWindow
    {
        private List<string> _categories;
        private List<CodeData> _codeDatas;
        private int _selectedCategory = -1;
        public Vector2[] sliderValue = new Vector2[10];



        private void TryInit(Vector2 windowSize)
        {
            if (DataReader.CodeData == null || DataReader.CodeData.Count == 0)
            {
                DataReader.LoadData();
                return;
            }

            if (_codeDatas == null || _codeDatas.Count == 0)
            {
                _codeDatas = DataReader.CodeData;
                return;
            }

            if (_codeDatas.Count == DataReader.CodeData.Count)
            {
                if (_categories != null && _categories.Count > 0) return;
            }

            _categories = new List<string>();

            for (int i = 0; i < _codeDatas.Count; i++)
            {
                if (!_categories.Contains(_codeDatas[i].category))
                {
                    _categories.Add(_codeDatas[i].category);
                }
            }
        }

        public void Draw(Vector2 windowSize)
        {
            TryInit(windowSize);
            if (_categories == null || _categories.Count == 0) return;

            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(10);
                DrawTemplateLister(windowSize, _categories.ToArray());
                DrawCategory(windowSize);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawTemplateLister(Vector2 windowSize, string[] items)
        {
            Rect currentRect = GUILayoutUtility.GetRect(0, 0);
            float boxWidth = 140;
            GUI.Box(new Rect(currentRect.x, currentRect.y, boxWidth, windowSize.y - 40), "",
                VGUIStyle.GetBoxStyle(VGUIStyle.BackgroundSoft));


            GUILayout.Space(5);
            GUILayout.BeginVertical(GUILayout.Width(boxWidth - 15), GUILayout.Height(windowSize.y - 60));
            for (int i = 0; i < items.Length; i++)
            {
                GUILayout.Space(5);

                if (GUILayout.Button(items[i], GUILayout.Height(30)))
                {
                    _selectedCategory = i;
                }
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Force Reload", GUILayout.Height(30)))
            {
                _selectedCategory = -1;
                CodeEditorDrawer.Reset();
                DataReader.LoadData();
                _categories = null;
                _codeDatas = null;
            }

            GUILayout.EndVertical();
        }

        private void DrawCategory(Vector2 window)
        {
            if (_selectedCategory == -1) return;
            CodeData[] datas = _codeDatas.Where(x => x.category == _categories[_selectedCategory]).ToArray();
            GUILayout.Space(20);

            GUILayout.BeginVertical();
            {
                GUILayout.Space(20);

                sliderValue[_selectedCategory] =
                    GUILayout.BeginScrollView(sliderValue[_selectedCategory], GUILayout.Width(window.x - 200),
                        GUILayout.Height(window.y - 60), GUILayout.ExpandWidth(true));
                

                for (int i = 0; i < datas.Length; i++)
                {
                    GUILayout.Label(datas[i].header, EditorStyles.boldLabel);
                    GUILayout.BeginVertical(VGUIStyle.GetBoxStyle(VGUIStyle.BackgroundSoft), GUILayout.Width(400));
                    {
                        CodeEditorDrawer.DrawCodeLine(datas[i], window.x - 250);
                        GUILayout.Space(20);
                    }
                    GUILayout.EndVertical();
                    GUILayout.Space(20);
                    GUILayout.FlexibleSpace();
                }

                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();

        }


    }
}
