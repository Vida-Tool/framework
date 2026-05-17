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
            VidaPremiumGUI.DrawSectionHeader("Codes", "Reusable code snippets grouped by category.");
            if (_categories == null || _categories.Count == 0)
            {
                VidaPremiumGUI.DrawCenteredState(
                    "Code listesi hazırlanıyor...",
                    "Code data yüklendiğinde kategoriler burada görünecek.",
                    VidaPremiumGUI.GetPremiumTexture("icon-codes.png"));
                return;
            }

            GUILayout.BeginHorizontal();
            {
                DrawTemplateLister(windowSize, _categories.ToArray());
                GUILayout.Space(14f);
                DrawCategory(windowSize);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawTemplateLister(Vector2 windowSize, string[] items)
        {
            float boxWidth = 156f;
            float boxHeight = Mathf.Max(160f, windowSize.y - 104f);
            Rect sidebarRect = GUILayoutUtility.GetRect(boxWidth, boxHeight, GUILayout.Width(boxWidth), GUILayout.Height(boxHeight));
            VidaPremiumGUI.DrawFrame(sidebarRect, "frame-sidebar.png");

            Rect innerRect = VidaPremiumGUI.GetInnerRect(sidebarRect, 10f);
            GUI.BeginGroup(innerRect);
            GUILayout.BeginArea(new Rect(0f, 0f, innerRect.width, innerRect.height));
            {
                for (int i = 0; i < items.Length; i++)
                {
                    Rect itemRect = GUILayoutUtility.GetRect(boxWidth - 20f, 36f, GUILayout.ExpandWidth(true), GUILayout.Height(36f));
                    if (VidaPremiumGUI.DrawSidebarItem(itemRect, items[i], VidaPremiumGUI.GetPremiumTexture("icon-codes.png"), _selectedCategory == i))
                    {
                        _selectedCategory = i;
                    }

                    GUILayout.Space(5f);
                }

                GUILayout.FlexibleSpace();
                if (VidaPremiumGUI.DrawHeaderAction("Reload", VidaPremiumGUI.GetPremiumTexture("icon-reload.png"), boxWidth - 20f))
                {
                    _selectedCategory = -1;
                    CodeEditorDrawer.Reset();
                    DataReader.LoadData();
                    _categories = null;
                    _codeDatas = null;
                }
            }
            GUILayout.EndArea();
            GUI.EndGroup();
        }

        private void DrawCategory(Vector2 window)
        {
            if (_selectedCategory == -1)
            {
                VidaPremiumGUI.DrawCenteredState(
                    "Kategori seç",
                    "Soldaki listeden bir code kategorisi seçerek snippet içeriklerini görüntüleyebilirsin.",
                    VidaPremiumGUI.GetPremiumTexture("icon-codes.png"));
                return;
            }

            CodeData[] datas = _codeDatas.Where(x => x.category == _categories[_selectedCategory]).ToArray();

            GUILayout.BeginVertical(GUILayout.Width(1000));
            {
                sliderValue[_selectedCategory] =
                    GUILayout.BeginScrollView(sliderValue[_selectedCategory], GUILayout.Width(window.x - 200),
                        GUILayout.Height(window.y - 60), GUILayout.ExpandWidth(true));
                {
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
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();

        }


    }
}
