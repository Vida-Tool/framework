#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public enum PackageRowAction
    {
        None,
        ToggleDetails,
        DownloadLatest
    }

    public static class VidaPremiumGUI
    {
        public const float PanelGap = 1f;
        public const float HeaderHeight = 96f;
        public const float ContentPadding = 24f;

        private const float CompactWindowWidth = 900f;
        private const float CompactSidebarWidth = 68f;
        private const float RegularSidebarWidth = 184f;

        private const float SidebarItemHeight = 42f;
        private const float ActionButtonHeight = 32f;
        private const float PackageRowHeight = 76f;
        private const float PackageHeaderHeight = 34f;

        private static readonly Color WindowBackgroundColor = new Color32(0x20, 0x1F, 0x23, 0xFF);
        private static readonly Color SidebarBackgroundColor = new Color32(0x19, 0x18, 0x1C, 0xFF);
        private static readonly Color HeaderBackgroundColor = new Color32(0x20, 0x1F, 0x23, 0xFF);
        private static readonly Color ContentBackgroundFallbackColor = new Color32(0x20, 0x1F, 0x23, 0xFF);
        private static readonly Color SurfaceColor = new Color32(0x25, 0x23, 0x29, 0xFF);
        private static readonly Color RaisedSurfaceColor = new Color32(0x2D, 0x2A, 0x33, 0xFF);
        private static readonly Color HoverSurfaceColor = new Color32(0x2A, 0x27, 0x30, 0xFF);
        private static readonly Color BorderColor = new Color32(0x35, 0x32, 0x3A, 0xFF);
        private static readonly Color HeaderTextColor = new Color32(0xF3, 0xF0, 0xEA, 0xFF);
        private static readonly Color BodyTextColor = new Color32(0xD6, 0xD1, 0xC9, 0xFF);
        private static readonly Color MutedTextColor = new Color32(0x96, 0x90, 0x9B, 0xFF);
        private static readonly Color AccentColor = new Color32(0xA5, 0x92, 0xDA, 0xFF);
        private static readonly Color SuccessColor = new Color32(0x78, 0xC6, 0x98, 0xFF);
        private static readonly Color WarningColor = new Color32(0xD8, 0xB4, 0x6A, 0xFF);
        private static readonly Color DangerColor = new Color32(0xD9, 0x78, 0x73, 0xFF);

        private static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
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
        private static GUIStyle _inlineMessageStyle;
        private static GUIStyle _infoIconStyle;

        public static void DrawWindowBackground(Rect rect)
        {
            EditorGUI.DrawRect(rect, WindowBackgroundColor);
        }

        public static void DrawSidebarBackground(Rect rect)
        {
            EditorGUI.DrawRect(rect, SidebarBackgroundColor);
        }

        public static void DrawHeaderBackground(Rect rect)
        {
            EditorGUI.DrawRect(rect, HeaderBackgroundColor);
        }

        public static void DrawContentBackground(Rect rect)
        {
            EditorGUI.DrawRect(rect, ContentBackgroundFallbackColor);
        }

        public static void DrawFrame(Rect rect, string frameName)
        {
            if (frameName == "frame-row.png" || frameName == "frame-header.png")
            {
                EditorGUI.DrawRect(new Rect(rect.x + 8f, rect.yMax - 1f, Mathf.Max(0f, rect.width - 16f), 1f), BorderColor);
                return;
            }

            bool selected = frameName == "frame-panel-selected.png";
            Color background = selected ? new Color32(0x49, 0x3C, 0x68, 0xFF) : GetFrameBackground(frameName);
            DrawRoundedRect(rect, background, selected ? 10f : 8f);
        }

        public static void DrawRoundedRect(Rect rect, Color color, float radius = 8f)
        {
            if (Event.current.type != EventType.Repaint || rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0f,
                color, 0f, Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * 0.5f));
        }

        public static void DrawBodyText(string text, bool muted = false)
        {
            GUILayout.Label(text, muted ? SectionSubtitleStyle : BodyStyle);
        }

        private static GUIStyle _bodyStyle;
        private static GUIStyle BodyStyle => _bodyStyle ?? (_bodyStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
        {
            fontSize = 12,
            normal = { textColor = BodyTextColor }
        });

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
            return new Rect(rect.x + padding, rect.y + padding, Mathf.Max(0f, rect.width - padding * 2f), Mathf.Max(0f, rect.height - padding * 2f));
        }

        public static bool IsCompact(float windowWidth)
        {
            return windowWidth < CompactWindowWidth;
        }

        public static float GetSidebarWidth(float windowWidth)
        {
            return IsCompact(windowWidth) ? CompactSidebarWidth : RegularSidebarWidth;
        }

        public static void DrawBrandHeader(Texture2D texture, bool isCompact)
        {
            float iconSize = isCompact ? 42f : 46f;
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (texture != null)
                {
                    GUILayout.Label(texture, GUILayout.Width(iconSize), GUILayout.Height(iconSize));
                }

                if (!isCompact)
                {
                    GUILayout.Space(10f);
                    using (new GUILayout.VerticalScope())
                    {
                        GUILayout.Space(5f);
                        GUILayout.Label("VIDA", BrandTitleStyle);
                        GUILayout.Label("Framework", BrandSubtitleStyle);
                    }
                }

                GUILayout.FlexibleSpace();
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

        public static bool DrawSidebarItem(Rect rect, string label, Texture2D icon, bool isSelected, bool iconOnly = false)
        {
            bool isHover = rect.Contains(Event.current.mousePosition);
            string frame = isSelected ? "frame-panel-selected.png" : isHover ? "frame-row-hover.png" : "frame-row.png";
            if (isSelected || isHover)
            {
                DrawFrame(rect, frame);
            }

            bool clicked = GUI.Button(rect, new GUIContent(string.Empty, label), GUIStyle.none);
            float iconSize = iconOnly ? 26f : 24f;
            Rect iconRect = iconOnly
                ? new Rect(rect.center.x - iconSize * 0.5f, rect.center.y - iconSize * 0.5f, iconSize, iconSize)
                : new Rect(rect.x + 12f, rect.center.y - iconSize * 0.5f, iconSize, iconSize);
            Rect labelRect = new Rect(rect.x + 48f, rect.y + 1f, rect.width - 56f, rect.height - 2f);

            if (icon != null)
            {
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }

            if (!iconOnly)
            {
                GUI.Label(labelRect, label, isSelected ? SidebarSelectedLabelStyle : SidebarLabelStyle);
            }
            else
            {
                GUI.Label(rect, new GUIContent(string.Empty, label));
            }

            return clicked;
        }

        public static bool DrawHeaderAction(string label, Texture2D icon, float width, bool isPrimary = false, bool isDanger = false, bool iconOnly = false, bool quiet = false)
        {
            Rect rect = GUILayoutUtility.GetRect(width, ActionButtonHeight, GUILayout.Width(width), GUILayout.Height(ActionButtonHeight));
            bool isHover = rect.Contains(Event.current.mousePosition);
            string frame = isPrimary ? "frame-button-primary.png" : isDanger ? "frame-button-danger.png" : "frame-button-secondary.png";
            if (!quiet || isHover)
            {
                DrawFrame(rect, frame);
            }

            bool clicked = GUI.Button(rect, new GUIContent(string.Empty, label), GUIStyle.none);
            if (isHover)
            {
                DrawHoverTint(rect, isPrimary ? 0.12f : 0.075f);
            }

            Rect iconRect = iconOnly
                ? new Rect(rect.center.x - 10f, rect.center.y - 10f, 20f, 20f)
                : new Rect(rect.x + 8f, rect.y + 6f, 20f, 20f);
            Rect labelRect = new Rect(rect.x + 31f, rect.y, rect.width - 36f, rect.height);

            if (icon != null)
            {
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }

            if (!iconOnly)
            {
                GUI.Label(labelRect, label, isPrimary ? ActionPrimaryLabelStyle : ActionLabelStyle);
            }
            else
            {
                GUI.Label(rect, new GUIContent(string.Empty, label));
            }

            return clicked;
        }

        public static bool DrawUpdateAction(float width, bool compact, bool busy)
        {
            Rect rect = GUILayoutUtility.GetRect(width, 44f, GUILayout.Width(width), GUILayout.Height(44f));
            DrawFrame(rect, "frame-button-secondary.png");
            string title = busy ? "Kontrol ediliyor…" : "Güncellemeleri kontrol et";
            string tooltip = VidaFramework.Connection ? title : "Güncellemeleri kontrol etmek için giriş yap";
            bool clicked = GUI.Button(rect, new GUIContent(string.Empty, tooltip), GUIStyle.none);
            Texture2D icon = GetPremiumTexture("icon-reload.png");
            Rect iconRect = new Rect(compact ? rect.center.x - 10f : rect.x + 10f, rect.center.y - 10f, 20f, 20f);
            if (icon != null) GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            if (!compact)
                GUI.Label(new Rect(rect.x + 36f, rect.y + 6f, rect.width - 40f, 32f),
                    busy ? "Kontrol ediliyor…" : "Güncellemeleri\nkontrol et", ActionLabelStyle);
            return clicked;
        }

        public static string DrawSearchField(string searchText, float width)
        {
            Rect rect = GUILayoutUtility.GetRect(width, ActionButtonHeight, GUILayout.Width(width), GUILayout.Height(ActionButtonHeight));
            DrawFrame(rect, "frame-search.png");

            Rect textRect = new Rect(rect.x + 12f, rect.y + 6f, rect.width - 24f, 20f);
            return GUI.TextField(textRect, searchText, SearchFieldStyle);
        }

        public static string DrawPasswordField(string text, float width)
        {
            Rect rect = GUILayoutUtility.GetRect(width, ActionButtonHeight, GUILayout.Width(width), GUILayout.Height(ActionButtonHeight));
            DrawFrame(rect, "frame-search.png");

            Rect textRect = new Rect(rect.x + 12f, rect.y + 6f, rect.width - 24f, 20f);
            return GUI.PasswordField(textRect, text, '\u2022', SearchFieldStyle);
        }

        public static void DrawInlineMessage(string message, bool hasError)
        {
            Color previousColor = GUI.color;
            GUI.color = hasError ? DangerColor : MutedTextColor;
            GUILayout.Label(message, InlineMessageStyle);
            GUI.color = previousColor;
        }

        public static void DrawConnectionStatus(bool isConnected, bool isRefreshing, bool iconOnly = false)
        {
            string textureName = isRefreshing ? "status-refreshing.png" : isConnected ? "status-connected.png" : "status-disconnected.png";
            string label = isRefreshing ? "Refreshing" : isConnected ? "Connected" : "Offline";
            Color labelColor = isRefreshing ? WarningColor : isConnected ? SuccessColor : DangerColor;

            float width = iconOnly ? ActionButtonHeight : 118f;
            Rect rect = GUILayoutUtility.GetRect(width, ActionButtonHeight, GUILayout.Width(width), GUILayout.Height(ActionButtonHeight));
            Texture2D icon = GetPremiumTexture(textureName);
            if (icon != null)
            {
                float iconX = iconOnly ? rect.center.x - 8f : rect.x + 9f;
                GUI.DrawTexture(new Rect(iconX, rect.y + 8f, 16f, 16f), icon, ScaleMode.ScaleToFit);
            }

            if (!iconOnly)
            {
                Color previousColor = GUI.color;
                GUI.color = labelColor;
                GUI.Label(new Rect(rect.x + 30f, rect.y, rect.width - 36f, rect.height), label, ChipLabelStyle);
                GUI.color = previousColor;
            }
            else
            {
                GUI.Label(rect, new GUIContent(string.Empty, label));
            }
        }

        public static void DrawSectionHeader(string title, string subtitle)
        {
            GUILayout.Label(title, SectionTitleStyle);
            GUILayout.Label(subtitle, SectionSubtitleStyle);
            GUILayout.Space(18f);
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

            StarterPackageInfoExtensions.GetColumnWidths(rect.width, out float categoryWidth, out float nameWidth, out float versionWidth, out float actionWidth);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, nameWidth, 18f), "PACKAGE", TableHeaderStyle);
            if (categoryWidth > 0f)
            {
                GUI.Label(new Rect(rect.x + 12f + nameWidth, rect.y + 8f, categoryWidth, 18f), "CATEGORY", TableHeaderStyle);
            }
            GUI.Label(new Rect(rect.x + 12f + nameWidth + categoryWidth, rect.y + 8f, versionWidth, 18f), "VERSION", TableHeaderStyle);
            GUI.Label(new Rect(rect.xMax - actionWidth - 12f, rect.y + 8f, actionWidth, 18f), "ACTIONS", TableHeaderStyle);
        }

        public static PackageRowAction DrawPackageRow(PackageDisplayInfo displayInfo, float windowWidth, bool isDisabled, bool isExpanded)
        {
            Rect rect = GetFullWidthRect(PackageRowHeight, windowWidth);
            bool isHover = !isDisabled && rect.Contains(Event.current.mousePosition);
            DrawFrame(rect, isExpanded ? "frame-panel-selected.png" : isHover ? "frame-row-hover.png" : "frame-row.png");

            StarterPackageInfoExtensions.GetColumnWidths(rect.width, out float categoryWidth, out float nameWidth, out float versionWidth, out float actionWidth);
            float x = rect.x + 12f;
            Rect iconRect = new Rect(x, rect.center.y - 20f, 40f, 40f);
            DrawRoundedRect(iconRect, RaisedSurfaceColor);
            Texture2D icon = GetPremiumTexture("icon-starter.png");
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(x + 9f, iconRect.y + 9f, 22f, 22f), icon, ScaleMode.ScaleToFit);
            }
            GUI.Label(new Rect(x + 52f, rect.y + 17f, nameWidth - 60f, 22f), new GUIContent(displayInfo.Name, displayInfo.Name), RowLabelStyle);
            string subtitle = categoryWidth > 0f ? "Unity package" : displayInfo.Category + " · Unity package";
            GUI.Label(new Rect(x + 52f, rect.y + 41f, nameWidth - 60f, 18f), subtitle, RowMutedLabelStyle);
            x += nameWidth;
            if (categoryWidth > 0f)
            {
                GUI.Label(new Rect(x, rect.y + 28f, categoryWidth - 8f, 20f), displayInfo.Category, RowMutedLabelStyle);
            }
            x += categoryWidth;
            GUI.Label(new Rect(x, rect.y + 28f, versionWidth - 8f, 20f), new GUIContent(string.IsNullOrEmpty(displayInfo.Version) ? "—" : displayInfo.Version, displayInfo.Version), RowMutedLabelStyle);

            const float actionGap = 8f;
            float buttonWidth = (actionWidth - actionGap) * 0.5f;
            Rect detailsRect = new Rect(rect.xMax - actionWidth - 12f, rect.center.y - 17f, buttonWidth, 34f);
            Rect downloadRect = new Rect(detailsRect.xMax + actionGap, detailsRect.y, buttonWidth, detailsRect.height);
            using (new EditorGUI.DisabledScope(isDisabled))
            {
                if (DrawPackageActionButton(detailsRect, "Details", null, true, AccentColor))
                {
                    return PackageRowAction.ToggleDetails;
                }

                if (DrawPackageActionButton(downloadRect, "Download", GetPremiumTexture("icon-download.png"), false, SuccessColor))
                {
                    return PackageRowAction.DownloadLatest;
                }
            }

            return PackageRowAction.None;
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

        public static void ResetStyles()
        {
            _bodyStyle = null;
            _brandTitleStyle = null;
            _brandSubtitleStyle = null;
            _sectionTitleStyle = null;
            _sectionSubtitleStyle = null;
            _sidebarLabelStyle = null;
            _sidebarSelectedLabelStyle = null;
            _tableHeaderStyle = null;
            _rowLabelStyle = null;
            _rowMutedLabelStyle = null;
            _centerTitleStyle = null;
            _centerSubtitleStyle = null;
            _actionLabelStyle = null;
            _actionPrimaryLabelStyle = null;
            _chipLabelStyle = null;
            _segmentLabelStyle = null;
            _segmentSelectedLabelStyle = null;
            _searchFieldStyle = null;
            _inlineMessageStyle = null;
            _infoIconStyle = null;
        }

        private static bool DrawPackageActionButton(Rect rect, string label, Texture2D icon, bool drawInfoIcon, Color background)
        {
            Color buttonColor = GUI.enabled ? background : new Color(background.r, background.g, background.b, 0.42f);
            DrawRoundedRect(rect, buttonColor, 7f);
            bool isHover = GUI.enabled && rect.Contains(Event.current.mousePosition);
            bool clicked = GUI.Button(rect, new GUIContent(string.Empty, label), GUIStyle.none);
            if (isHover)
            {
                DrawHoverTint(rect, 0.15f);
            }

            Rect iconRect = new Rect(rect.x + 9f, rect.y + 7f, 20f, 20f);
            if (drawInfoIcon)
            {
                DrawRoundedRect(new Rect(iconRect.x + 2f, iconRect.y + 2f, 16f, 16f), new Color32(0x32, 0x28, 0x49, 0xC8), 8f);
                GUI.Label(iconRect, "i", InfoIconStyle);
            }
            else if (icon != null)
            {
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }

            GUI.Label(new Rect(rect.x + 33f, rect.y, rect.width - 39f, rect.height), label, ActionPrimaryLabelStyle);
            return clicked;
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

            GUI.Label(new Rect(rect.x + (icon == null ? 14f : 31f), rect.y, rect.width - (icon == null ? 20f : 36f), rect.height), label, isPrimary ? ActionPrimaryLabelStyle : ActionLabelStyle);
            return clicked;
        }

        private static Rect GetFullWidthRect(float height, float fallbackWidth)
        {
            Rect rect = GUILayoutUtility.GetRect(1f, height, GUILayout.ExpandWidth(true), GUILayout.Height(height));
            float width = rect.width > 1f ? rect.width : fallbackWidth;
            rect.width = Mathf.Max(220f, width - 4f);
            return rect;
        }

        private static void DrawHoverTint(Rect rect, float alpha)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            DrawRoundedRect(rect, new Color(1f, 1f, 1f, alpha));
        }

        private static Color GetFrameBackground(string frameName)
        {
            switch (frameName)
            {
                case "frame-panel-selected.png":
                    return RaisedSurfaceColor;
                case "frame-row-hover.png":
                    return HoverSurfaceColor;
                case "frame-button-primary.png":
                    return AccentColor;
                case "frame-button-danger.png":
                    return new Color32(0x45, 0x24, 0x27, 0xFF);
                case "frame-button-secondary.png":
                case "frame-search.png":
                case "frame-chip.png":
                    return RaisedSurfaceColor;
                case "frame-header.png":
                    return new Color32(0x17, 0x16, 0x1A, 0xFF);
                case "frame-sidebar.png":
                case "frame-panel.png":
                case "frame-row.png":
                    return SurfaceColor;
                default:
                    return SurfaceColor;
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
                        alignment = TextAnchor.MiddleLeft,
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
                        alignment = TextAnchor.MiddleLeft,
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
                        fontSize = 24,
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
                        fontSize = 13,
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
                        fontSize = 13,
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
                        normal = { textColor = new Color32(0x20, 0x19, 0x30, 0xFF) }
                    };
                }

                return _actionPrimaryLabelStyle;
            }
        }

        private static GUIStyle InfoIconStyle
        {
            get
            {
                if (_infoIconStyle == null)
                {
                    _infoIconStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 12,
                        normal = { textColor = HeaderTextColor }
                    };
                }

                return _infoIconStyle;
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
                        normal = { textColor = HeaderTextColor }
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

        private static GUIStyle InlineMessageStyle
        {
            get
            {
                if (_inlineMessageStyle == null)
                {
                    _inlineMessageStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
                    {
                        normal = { textColor = new Color32(0x20, 0x19, 0x30, 0xFF) }
                    };
                }

                return _inlineMessageStyle;
            }
        }
    }
}
#endif
