#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public static class VidaPremiumGUI
    {
        public const float OuterPadding = 12f;
        public const float SidebarWidth = 174f;
        public const float HeaderHeight = 66f;
        public const float ContentPadding = 14f;

        private const float SidebarItemHeight = 42f;
        private const float ActionButtonHeight = 32f;
        private const float PackageRowHeight = 48f;
        private const float PackageHeaderHeight = 34f;

        private static readonly Color WindowBackgroundColor = new Color32(0x12, 0x16, 0x1E, 0xFF);
        private static readonly Color ContentBackgroundFallbackColor = new Color32(0x1D, 0x24, 0x30, 0xF2);
        private static readonly Color HeaderTextColor = new Color32(0xF0, 0xF5, 0xFF, 0xFF);
        private static readonly Color BodyTextColor = new Color32(0xD7, 0xE0, 0xEC, 0xFF);
        private static readonly Color MutedTextColor = new Color32(0x94, 0xA2, 0xB5, 0xFF);
        private static readonly Color AccentColor = new Color32(0x4C, 0xC6, 0xFF, 0xFF);
        private static readonly Color SuccessColor = new Color32(0x5B, 0xD8, 0x8E, 0xFF);
        private static readonly Color WarningColor = new Color32(0xF5, 0xC5, 0x52, 0xFF);
        private static readonly Color DangerColor = new Color32(0xFF, 0x6B, 0x60, 0xFF);

        private static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, GUIStyle> FrameStyleCache = new Dictionary<string, GUIStyle>();

        private static GUIStyle _brandTitleStyle;
        private static GUIStyle _brandSubtitleStyle;
        private static GUIStyle _sectionTitleStyle;
        private static GUIStyle _sectionSubtitleStyle;
        private static GUIStyle _sidebarLabelStyle;
        private static GUIStyle _sidebarSelectedLabelStyle;
        private static GUIStyle _tableHeaderStyle;
        private static GUIStyle _rowLabelStyle;
        private static GUIStyle _rowMutedLabelStyle;
        private static GUIStyle _centerTitleStyle;
        private static GUIStyle _centerSubtitleStyle;
        private static GUIStyle _actionLabelStyle;
        private static GUIStyle _actionPrimaryLabelStyle;
        private static GUIStyle _chipLabelStyle;
        private static GUIStyle _segmentLabelStyle;
        private static GUIStyle _segmentSelectedLabelStyle;
        private static GUIStyle _searchFieldStyle;

        public static void DrawWindowBackground(Rect rect)
        {
            EditorGUI.DrawRect(rect, WindowBackgroundColor);

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.04f);

            for (float x = rect.x + 28f; x < rect.xMax; x += 42f)
            {
                EditorGUI.DrawRect(new Rect(x, rect.y, 1f, rect.height), Color.white);
            }

            for (float y = rect.y + 26f; y < rect.yMax; y += 42f)
            {
                EditorGUI.DrawRect(new Rect(rect.x, y, rect.width, 1f), Color.white);
            }

            GUI.color = previousColor;
        }

        public static void DrawFrame(Rect rect, string frameName)
        {
            GUIStyle style = GetFrameStyle(frameName);
            if (style == null)
            {
                EditorGUI.DrawRect(rect, ContentBackgroundFallbackColor);
                return;
            }

            GUI.Box(rect, GUIContent.none, style);
        }

        public static void DrawSidebarLogo(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            float width = 112f;
            float height = width * texture.height / texture.width;
            Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.62f);
            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
            GUI.color = previousColor;
        }

        public static Rect GetInnerRect(Rect rect, float padding = ContentPadding)
        {
            return new Rect(rect.x + padding, rect.y + padding, rect.width - padding * 2f, rect.height - padding * 2f);
        }

        public static void DrawBrandHeader()
        {
            using (new GUILayout.VerticalScope())
            {
                GUILayout.Label("Vida", BrandTitleStyle);
                GUILayout.Label("Framework", BrandSubtitleStyle);
            }
        }

        public static int DrawSegmentedControl(string[] options, int selectedIndex, float maxWidth)
        {
            int nextIndex = selectedIndex;
            float width = Mathf.Min(maxWidth, Mathf.Max(160f, options.Length * 94f));
            Rect rect = GUILayoutUtility.GetRect(width, ActionButtonHeight, GUILayout.Width(width), GUILayout.Height(ActionButtonHeight));
            float itemWidth = rect.width / Mathf.Max(1, options.Length);

            for (int i = 0; i < options.Length; i++)
            {
                Rect itemRect = new Rect(rect.x + itemWidth * i, rect.y, itemWidth - 3f, rect.height);
                bool isSelected = i == selectedIndex;
                bool isHover = itemRect.Contains(Event.current.mousePosition);
                string frame = isSelected ? "frame-panel-selected.png" : isHover ? "frame-row-hover.png" : "frame-button-secondary.png";
                DrawFrame(itemRect, frame);

                if (isHover && !isSelected)
                {
                    DrawHoverTint(itemRect, 0.05f);
                }

                if (GUI.Button(itemRect, GUIContent.none, GUIStyle.none))
                {
                    nextIndex = i;
                }

                GUI.Label(itemRect, options[i], isSelected ? SegmentSelectedLabelStyle : SegmentLabelStyle);
            }

            return nextIndex;
        }

        public static bool DrawSidebarItem(Rect rect, string label, Texture2D icon, bool isSelected)
        {
            bool isHover = rect.Contains(Event.current.mousePosition);
            string frame = isSelected ? "frame-panel-selected.png" : isHover ? "frame-row-hover.png" : "frame-row.png";
            DrawFrame(rect, frame);

            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            Rect iconRect = new Rect(rect.x + 10f, rect.y + 6f, 30f, 30f);
            Rect labelRect = new Rect(rect.x + 48f, rect.y + 1f, rect.width - 56f, rect.height - 2f);

            if (icon != null)
            {
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }

            GUI.Label(labelRect, label, isSelected ? SidebarSelectedLabelStyle : SidebarLabelStyle);
            return clicked;
        }

        public static bool DrawHeaderAction(string label, Texture2D icon, float width, bool isPrimary = false, bool isDanger = false)
        {
            Rect rect = GUILayoutUtility.GetRect(width, ActionButtonHeight, GUILayout.Width(width), GUILayout.Height(ActionButtonHeight));
            bool isHover = rect.Contains(Event.current.mousePosition);
            string frame = isPrimary ? "frame-button-primary.png" : isDanger ? "frame-button-danger.png" : "frame-button-secondary.png";
            DrawFrame(rect, frame);

            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            if (isHover)
            {
                DrawHoverTint(rect, isPrimary ? 0.12f : 0.075f);
            }

            Rect iconRect = new Rect(rect.x + 8f, rect.y + 6f, 20f, 20f);
            Rect labelRect = new Rect(rect.x + 31f, rect.y, rect.width - 36f, rect.height);

            if (icon != null)
            {
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }

            GUI.Label(labelRect, label, isPrimary ? ActionPrimaryLabelStyle : ActionLabelStyle);
            return clicked;
        }

        public static string DrawSearchField(string searchText, float width)
        {
            Rect rect = GUILayoutUtility.GetRect(width, ActionButtonHeight, GUILayout.Width(width), GUILayout.Height(ActionButtonHeight));
            DrawFrame(rect, "frame-search.png");

            Rect textRect = new Rect(rect.x + 12f, rect.y + 6f, rect.width - 24f, 20f);
            return GUI.TextField(textRect, searchText, SearchFieldStyle);
        }

        public static void DrawConnectionStatus(bool isConnected, bool isRefreshing)
        {
            string textureName = isRefreshing ? "status-refreshing.png" : isConnected ? "status-connected.png" : "status-disconnected.png";
            string label = isRefreshing ? "Refreshing" : isConnected ? "Connected" : "Offline";
            Color labelColor = isRefreshing ? WarningColor : isConnected ? SuccessColor : DangerColor;

            Rect rect = GUILayoutUtility.GetRect(118f, ActionButtonHeight, GUILayout.Width(118f), GUILayout.Height(ActionButtonHeight));
            DrawFrame(rect, "frame-chip.png");

            Texture2D icon = GetPremiumTexture(textureName);
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(rect.x + 9f, rect.y + 8f, 16f, 16f), icon, ScaleMode.ScaleToFit);
            }

            Color previousColor = GUI.color;
            GUI.color = labelColor;
            GUI.Label(new Rect(rect.x + 30f, rect.y, rect.width - 36f, rect.height), label, ChipLabelStyle);
            GUI.color = previousColor;
        }

        public static void DrawSectionHeader(string title, string subtitle)
        {
            GUILayout.Label(title, SectionTitleStyle);
            GUILayout.Label(subtitle, SectionSubtitleStyle);
            GUILayout.Space(10f);
        }

        public static void DrawHeaderInfo(string title, string subtitle)
        {
            GUILayout.Label(title, SectionTitleStyle);
            GUILayout.Label(subtitle, SectionSubtitleStyle);
        }

        public static void DrawPackageTableHeader(float windowWidth)
        {
            Rect rect = GetFullWidthRect(PackageHeaderHeight, windowWidth);
            DrawFrame(rect, "frame-header.png");

            StarterPackageInfoExtensions.GetColumnWidths(rect.width, out float categoryWidth, out float nameWidth, out float versionWidth, out float downloadWidth);
            float x = rect.x + 12f;
            float y = rect.y + 8f;

            GUI.Label(new Rect(x, y, categoryWidth - 12f, 18f), "Kategori", TableHeaderStyle);
            x += categoryWidth;
            GUI.Label(new Rect(x, y, nameWidth - 12f, 18f), "Paket adı", TableHeaderStyle);
            x += nameWidth;
            GUI.Label(new Rect(x, y, versionWidth - 12f, 18f), "Versiyon", TableHeaderStyle);
            GUI.Label(new Rect(rect.xMax - downloadWidth - 4f, y, downloadWidth, 18f), "İşlem", TableHeaderStyle);
        }

        public static bool DrawPackageRow(PackageDisplayInfo displayInfo, float windowWidth, bool isDisabled)
        {
            Rect rect = GetFullWidthRect(PackageRowHeight, windowWidth);
            bool isHover = !isDisabled && rect.Contains(Event.current.mousePosition);
            DrawFrame(rect, isHover ? "frame-row-hover.png" : "frame-row.png");

            StarterPackageInfoExtensions.GetColumnWidths(rect.width, out float categoryWidth, out float nameWidth, out float versionWidth, out float downloadWidth);
            Rect categoryRect = new Rect(rect.x + 10f, rect.y + 11f, Mathf.Max(66f, categoryWidth - 20f), 24f);
            DrawFrame(categoryRect, "frame-chip.png");

            GUI.Label(new Rect(categoryRect.x + 10f, categoryRect.y + 2f, categoryRect.width - 20f, 20f), displayInfo.Category, ChipLabelStyle);

            float x = rect.x + categoryWidth + 10f;
            GUI.Label(new Rect(x, rect.y + 8f, nameWidth - 18f, 20f), displayInfo.Name, RowLabelStyle);
            GUI.Label(new Rect(x, rect.y + 28f, nameWidth - 18f, 16f), "Unity package", RowMutedLabelStyle);

            x += nameWidth;
            GUI.Label(new Rect(x, rect.y + 15f, versionWidth - 12f, 18f), string.IsNullOrEmpty(displayInfo.Version) ? "-" : displayInfo.Version, RowMutedLabelStyle);

            Rect buttonRect = new Rect(rect.xMax - downloadWidth - 4f, rect.y + 8f, downloadWidth, 32f);
            using (new EditorGUI.DisabledScope(isDisabled))
            {
                return DrawInlineActionButton(buttonRect, "İndir", GetPremiumTexture("icon-download.png"), true);
            }
        }

        public static void DrawCenteredState(string title, string subtitle, Texture2D icon = null)
        {
            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new GUILayout.VerticalScope(GUILayout.Width(360f)))
                {
                    if (icon != null)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            GUILayout.Label(icon, GUILayout.Width(48f), GUILayout.Height(48f));
                            GUILayout.FlexibleSpace();
                        }
                    }

                    GUILayout.Label(title, CenterTitleStyle);
                    GUILayout.Space(4f);
                    GUILayout.Label(subtitle, CenterSubtitleStyle);
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();
        }

        public static bool DrawRetryState(string title, string subtitle)
        {
            bool clicked = false;
            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new GUILayout.VerticalScope(GUILayout.Width(420f)))
                {
                    GUILayout.Label(title, CenterTitleStyle);
                    GUILayout.Space(4f);
                    GUILayout.Label(subtitle, CenterSubtitleStyle);
                    GUILayout.Space(14f);
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        Rect buttonRect = GUILayoutUtility.GetRect(140f, ActionButtonHeight, GUILayout.Width(140f), GUILayout.Height(ActionButtonHeight));
                        clicked = DrawInlineActionButton(buttonRect, "Tekrar Dene", GetPremiumTexture("icon-reload.png"), false);
                        GUILayout.FlexibleSpace();
                    }
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();
            return clicked;
        }

        public static Texture2D GetPremiumTexture(string fileName)
        {
            if (TextureCache.TryGetValue(fileName, out Texture2D texture))
            {
                return texture;
            }

            texture = TextureLoader.GetTexture("Premium/" + fileName);
            TextureCache[fileName] = texture;
            return texture;
        }

        private static bool DrawInlineActionButton(Rect rect, string label, Texture2D icon, bool isPrimary)
        {
            bool isHover = GUI.enabled && rect.Contains(Event.current.mousePosition);
            DrawFrame(rect, isPrimary ? "frame-button-primary.png" : "frame-button-secondary.png");
            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            if (isHover)
            {
                DrawHoverTint(rect, isPrimary ? 0.12f : 0.075f);
            }

            if (icon != null)
            {
                GUI.DrawTexture(new Rect(rect.x + 8f, rect.y + 6f, 20f, 20f), icon, ScaleMode.ScaleToFit);
            }

            GUI.Label(new Rect(rect.x + 31f, rect.y, rect.width - 36f, rect.height), label, isPrimary ? ActionPrimaryLabelStyle : ActionLabelStyle);
            return clicked;
        }

        private static Rect GetFullWidthRect(float height, float fallbackWidth)
        {
            Rect rect = GUILayoutUtility.GetRect(1f, height, GUILayout.ExpandWidth(true), GUILayout.Height(height));
            float width = rect.width > 1f ? rect.width : fallbackWidth;
            rect.width = Mathf.Max(280f, width - 18f);
            return rect;
        }

        private static void DrawHoverTint(Rect rect, float alpha)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previousColor;
        }

        private static GUIStyle GetFrameStyle(string frameName)
        {
            if (FrameStyleCache.TryGetValue(frameName, out GUIStyle style))
            {
                return style;
            }

            Texture2D texture = GetPremiumTexture(frameName);
            if (texture == null)
            {
                return null;
            }

            style = new GUIStyle(GUIStyle.none)
            {
                normal = { background = texture },
                border = GetFrameBorder(frameName),
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                overflow = new RectOffset(0, 0, 0, 0),
                stretchWidth = true,
                stretchHeight = true
            };

            FrameStyleCache[frameName] = style;
            return style;
        }

        private static RectOffset GetFrameBorder(string frameName)
        {
            switch (frameName)
            {
                case "frame-panel.png":
                case "frame-panel-selected.png":
                case "frame-sidebar.png":
                    return new RectOffset(20, 20, 20, 20);
                case "frame-header.png":
                    return new RectOffset(16, 16, 16, 16);
                case "frame-button-primary.png":
                case "frame-button-secondary.png":
                case "frame-button-danger.png":
                case "frame-row.png":
                case "frame-row-hover.png":
                case "frame-search.png":
                    return new RectOffset(12, 12, 12, 12);
                case "frame-chip.png":
                    return new RectOffset(10, 10, 10, 10);
                default:
                    return new RectOffset(12, 12, 12, 12);
            }
        }

        private static GUIStyle BrandTitleStyle
        {
            get
            {
                if (_brandTitleStyle == null)
                {
                    _brandTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 16,
                        normal = { textColor = HeaderTextColor }
                    };
                }

                return _brandTitleStyle;
            }
        }

        private static GUIStyle BrandSubtitleStyle
        {
            get
            {
                if (_brandSubtitleStyle == null)
                {
                    _brandSubtitleStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = MutedTextColor }
                    };
                }

                return _brandSubtitleStyle;
            }
        }

        private static GUIStyle SectionTitleStyle
        {
            get
            {
                if (_sectionTitleStyle == null)
                {
                    _sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 18,
                        normal = { textColor = HeaderTextColor }
                    };
                }

                return _sectionTitleStyle;
            }
        }

        private static GUIStyle SectionSubtitleStyle
        {
            get
            {
                if (_sectionSubtitleStyle == null)
                {
                    _sectionSubtitleStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
                    {
                        normal = { textColor = MutedTextColor }
                    };
                }

                return _sectionSubtitleStyle;
            }
        }

        private static GUIStyle SidebarLabelStyle
        {
            get
            {
                if (_sidebarLabelStyle == null)
                {
                    _sidebarLabelStyle = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 12,
                        normal = { textColor = BodyTextColor }
                    };
                }

                return _sidebarLabelStyle;
            }
        }

        private static GUIStyle SidebarSelectedLabelStyle
        {
            get
            {
                if (_sidebarSelectedLabelStyle == null)
                {
                    _sidebarSelectedLabelStyle = new GUIStyle(SidebarLabelStyle)
                    {
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = HeaderTextColor }
                    };
                }

                return _sidebarSelectedLabelStyle;
            }
        }

        private static GUIStyle TableHeaderStyle
        {
            get
            {
                if (_tableHeaderStyle == null)
                {
                    _tableHeaderStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                    {
                        normal = { textColor = MutedTextColor }
                    };
                }

                return _tableHeaderStyle;
            }
        }

        private static GUIStyle RowLabelStyle
        {
            get
            {
                if (_rowLabelStyle == null)
                {
                    _rowLabelStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        normal = { textColor = HeaderTextColor }
                    };
                }

                return _rowLabelStyle;
            }
        }

        private static GUIStyle RowMutedLabelStyle
        {
            get
            {
                if (_rowMutedLabelStyle == null)
                {
                    _rowMutedLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = MutedTextColor }
                    };
                }

                return _rowMutedLabelStyle;
            }
        }

        private static GUIStyle CenterTitleStyle
        {
            get
            {
                if (_centerTitleStyle == null)
                {
                    _centerTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 16,
                        normal = { textColor = HeaderTextColor }
                    };
                }

                return _centerTitleStyle;
            }
        }

        private static GUIStyle CenterSubtitleStyle
        {
            get
            {
                if (_centerSubtitleStyle == null)
                {
                    _centerSubtitleStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = MutedTextColor }
                    };
                }

                return _centerSubtitleStyle;
            }
        }

        private static GUIStyle ActionLabelStyle
        {
            get
            {
                if (_actionLabelStyle == null)
                {
                    _actionLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        normal = { textColor = BodyTextColor }
                    };
                }

                return _actionLabelStyle;
            }
        }

        private static GUIStyle ActionPrimaryLabelStyle
        {
            get
            {
                if (_actionPrimaryLabelStyle == null)
                {
                    _actionPrimaryLabelStyle = new GUIStyle(ActionLabelStyle)
                    {
                        normal = { textColor = Color.white }
                    };
                }

                return _actionPrimaryLabelStyle;
            }
        }

        private static GUIStyle ChipLabelStyle
        {
            get
            {
                if (_chipLabelStyle == null)
                {
                    _chipLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        clipping = TextClipping.Clip,
                        normal = { textColor = AccentColor }
                    };
                }

                return _chipLabelStyle;
            }
        }

        private static GUIStyle SegmentLabelStyle
        {
            get
            {
                if (_segmentLabelStyle == null)
                {
                    _segmentLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = BodyTextColor }
                    };
                }

                return _segmentLabelStyle;
            }
        }

        private static GUIStyle SegmentSelectedLabelStyle
        {
            get
            {
                if (_segmentSelectedLabelStyle == null)
                {
                    _segmentSelectedLabelStyle = new GUIStyle(SegmentLabelStyle)
                    {
                        normal = { textColor = Color.white }
                    };
                }

                return _segmentSelectedLabelStyle;
            }
        }

        private static GUIStyle SearchFieldStyle
        {
            get
            {
                if (_searchFieldStyle == null)
                {
                    _searchFieldStyle = new GUIStyle(EditorStyles.textField)
                    {
                        border = new RectOffset(0, 0, 0, 0),
                        normal = { background = null, textColor = HeaderTextColor },
                        focused = { background = null, textColor = HeaderTextColor }
                    };
                }

                return _searchFieldStyle;
            }
        }
    }
}
#endif
