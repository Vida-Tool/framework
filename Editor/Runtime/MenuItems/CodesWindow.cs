using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Vida.Framework.CodeEditor;
using Vida.Framework.Editor;

namespace Vida.Framework
{
    public class CodesWindow
    {
        private const int AllCodeFilterIndex = -1;
        private const float CodeFilterHeight = 38f;
        private const float CodeFilterButtonHeight = 28f;
        private const float CodeFilterMinButtonWidth = 56f;
        private const float CodeFilterMaxButtonWidth = 180f;

        private static int _reloadRequestVersion;

        private List<string> _categories;
        private List<CodeData> _codeDatas;
        private readonly Dictionary<string, Vector2> _scrollPositions = new Dictionary<string, Vector2>();
        private readonly Dictionary<string, Vector2> _filterScrollPositions = new Dictionary<string, Vector2>();
        private readonly Dictionary<string, int> _selectedCodeIndexes = new Dictionary<string, int>();
        private int _selectedCategory = -1;
        private int _dataVersion = -1;
        private int _handledReloadRequestVersion = -1;
        private GUIStyle _filterLabelStyle;
        private GUIStyle _filterSelectedLabelStyle;

        public static void RequestReload()
        {
            _reloadRequestVersion++;
            DataReader.LoadData();
        }

        public void Draw(Vector2 windowSize)
        {
            TryInit();
            VidaPremiumGUI.DrawSectionHeader("Codes", "Reusable code snippets grouped by category.");
            if (_categories == null || _categories.Count == 0)
            {
                VidaPremiumGUI.DrawCenteredState(
                    DataReader.TaskCount > 0 ? "Code listesi yükleniyor..." : "Code bulunamadı",
                    DataReader.TaskCount > 0 ? "Code Info altındaki kategoriler hazırlanıyor." : "Code Info altında .txt veya .cs dosyası bulunamadı.",
                    VidaPremiumGUI.GetPremiumTexture("icon-codes.png"));
                return;
            }

            GUILayout.BeginHorizontal();
            {
                DrawCategoryList(windowSize, _categories.ToArray());
                GUILayout.Space(14f);
                DrawCategory(windowSize);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }

        private void TryInit()
        {
            TryHandleReloadRequest();

            if ((DataReader.CodeData == null || DataReader.CodeData.Count == 0) && DataReader.TaskCount == 0 && !DataReader.HasLoaded)
            {
                DataReader.LoadData();
                return;
            }

            if (DataReader.TaskCount > 0)
            {
                return;
            }

            if (_dataVersion == DataReader.DataVersion && _categories != null)
            {
                return;
            }

            _dataVersion = DataReader.DataVersion;
            _codeDatas = DataReader.CodeData;
            RefreshCategories();
        }

        private void RefreshCategories()
        {
            _categories = new List<string>();
            if (_codeDatas == null)
            {
                return;
            }

            for (int i = 0; i < _codeDatas.Count; i++)
            {
                if (!_categories.Contains(_codeDatas[i].category))
                {
                    _categories.Add(_codeDatas[i].category);
                }
            }

            if (_selectedCategory >= _categories.Count)
            {
                _selectedCategory = -1;
            }
        }

        private void DrawCategoryList(Vector2 windowSize, string[] items)
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
                    ReloadData();
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

            string category = _categories[_selectedCategory];
            List<CodeData> categoryCodes = GetCategoryCodes(category);
            int selectedCodeIndex = GetSelectedCodeIndex(category, categoryCodes);
            Vector2 scrollPosition = GetScrollPosition(category);
            float panelWidth = Mathf.Max(260f, window.x - 200);

            GUILayout.BeginVertical(GUILayout.Width(1000));
            {
                DrawCodeFilterToolbar(category, categoryCodes, selectedCodeIndex, panelWidth);
                GUILayout.Space(8f);

                scrollPosition =
                    GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(panelWidth),
                        GUILayout.Height(Mathf.Max(120f, window.y - 106)), GUILayout.ExpandWidth(true));
                {
                    for (int i = 0; i < categoryCodes.Count; i++)
                    {
                        if (selectedCodeIndex != AllCodeFilterIndex && selectedCodeIndex != i)
                        {
                            continue;
                        }

                        CodeEditorDrawer.DrawCodeLine(categoryCodes[i], window.x - 250);
                        GUILayout.Space(14);
                    }
                }
                GUILayout.EndScrollView();
                _scrollPositions[category] = scrollPosition;
            }
            GUILayout.EndVertical();
        }

        private void DrawCodeFilterToolbar(string category, List<CodeData> categoryCodes, int selectedCodeIndex, float width)
        {
            TryInitFilterStyles();

            Vector2 filterScrollPosition = GetFilterScrollPosition(category);
            filterScrollPosition = GUILayout.BeginScrollView(
                filterScrollPosition,
                false,
                false,
                GUI.skin.horizontalScrollbar,
                GUIStyle.none,
                GUIStyle.none,
                GUILayout.Width(width),
                GUILayout.Height(CodeFilterHeight));
            {
                GUILayout.BeginHorizontal();
                {
                    if (DrawCodeFilterButton("All", selectedCodeIndex == AllCodeFilterIndex, CodeFilterMinButtonWidth))
                    {
                        SetSelectedCodeIndex(category, AllCodeFilterIndex);
                    }

                    GUILayout.Space(6f);

                    for (int i = 0; i < categoryCodes.Count; i++)
                    {
                        float buttonWidth = GetCodeFilterButtonWidth(categoryCodes[i].header);
                        if (DrawCodeFilterButton(categoryCodes[i].header, selectedCodeIndex == i, buttonWidth))
                        {
                            SetSelectedCodeIndex(category, i);
                        }

                        GUILayout.Space(6f);
                    }

                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            _filterScrollPositions[category] = filterScrollPosition;
        }

        private bool DrawCodeFilterButton(string label, bool isSelected, float width)
        {
            Rect rect = GUILayoutUtility.GetRect(width, CodeFilterButtonHeight, GUILayout.Width(width), GUILayout.Height(CodeFilterButtonHeight));
            bool isHover = rect.Contains(Event.current.mousePosition);
            string frame = isSelected ? "frame-panel-selected.png" : isHover ? "frame-row-hover.png" : "frame-button-secondary.png";

            VidaPremiumGUI.DrawFrame(rect, frame);
            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            GUI.Label(rect, label, isSelected ? _filterSelectedLabelStyle : _filterLabelStyle);
            return clicked;
        }

        private void TryInitFilterStyles()
        {
            if (_filterLabelStyle != null)
            {
                return;
            }

            _filterLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                fontSize = 11
            };
            _filterLabelStyle.normal.textColor = new Color32(0xB7, 0xC5, 0xD8, 0xFF);

            _filterSelectedLabelStyle = new GUIStyle(_filterLabelStyle);
            _filterSelectedLabelStyle.normal.textColor = new Color32(0xF4, 0xFB, 0xFF, 0xFF);
        }

        private void TryHandleReloadRequest()
        {
            if (_handledReloadRequestVersion == _reloadRequestVersion)
            {
                return;
            }

            _handledReloadRequestVersion = _reloadRequestVersion;
            ResetLocalData();
        }

        private void ReloadData()
        {
            RequestReload();
            ResetLocalData();
        }

        private void ResetLocalData()
        {
            _selectedCategory = -1;
            _dataVersion = -1;
            _categories = null;
            _codeDatas = null;
            _scrollPositions.Clear();
            _filterScrollPositions.Clear();
            _selectedCodeIndexes.Clear();
            CodeEditorDrawer.Reset();
        }

        private Vector2 GetScrollPosition(string category)
        {
            if (_scrollPositions.TryGetValue(category, out Vector2 scrollPosition))
            {
                return scrollPosition;
            }

            return Vector2.zero;
        }

        private Vector2 GetFilterScrollPosition(string category)
        {
            if (_filterScrollPositions.TryGetValue(category, out Vector2 scrollPosition))
            {
                return scrollPosition;
            }

            return Vector2.zero;
        }

        private void SetSelectedCodeIndex(string category, int selectedCodeIndex)
        {
            _selectedCodeIndexes[category] = selectedCodeIndex;
            _scrollPositions[category] = Vector2.zero;
        }

        private int GetSelectedCodeIndex(string category, List<CodeData> categoryCodes)
        {
            if (!_selectedCodeIndexes.TryGetValue(category, out int selectedCodeIndex))
            {
                return AllCodeFilterIndex;
            }

            if (selectedCodeIndex < AllCodeFilterIndex || selectedCodeIndex >= categoryCodes.Count)
            {
                SetSelectedCodeIndex(category, AllCodeFilterIndex);
                return AllCodeFilterIndex;
            }

            return selectedCodeIndex;
        }

        private List<CodeData> GetCategoryCodes(string category)
        {
            List<CodeData> categoryCodes = new List<CodeData>();
            for (int i = 0; i < _codeDatas.Count; i++)
            {
                if (_codeDatas[i].category == category)
                {
                    categoryCodes.Add(_codeDatas[i]);
                }
            }

            return categoryCodes;
        }

        private float GetCodeFilterButtonWidth(string label)
        {
            TryInitFilterStyles();
            float width = _filterLabelStyle.CalcSize(new GUIContent(label)).x + 24f;
            return Mathf.Clamp(width, CodeFilterMinButtonWidth, CodeFilterMaxButtonWidth);
        }
    }
}
