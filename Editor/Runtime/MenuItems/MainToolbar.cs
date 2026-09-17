using UnityEditor;
using UnityEngine;
using Vida.Framework.CodeEditor;

namespace Vida.Framework.Editor
{
    public class MainToolbar
    {
        public static string search = "";
        private readonly ToolbarItem[] _items =
        {
            new ToolbarItem("Home", "icon-home.png", "Overview"),
            new ToolbarItem("Starter", "icon-starter.png", "Starter packs"),
            new ToolbarItem("SDK", "icon-sdk.png", "SDK packages"),
            new ToolbarItem("Codes", "icon-codes.png", "Code snippets"),
            new ToolbarItem("Packages", "icon-download.png", "Framework packages")
        };

        public void DrawSidebar(Rect sidebarRect, Texture2D logoTexture)
        {
            VidaPremiumGUI.DrawSidebarBackground(sidebarRect);

            bool isCompact = sidebarRect.width < 100f;
            float padding = isCompact ? 10f : 12f;
            Rect innerRect = VidaPremiumGUI.GetInnerRect(sidebarRect, padding);
            GUILayout.BeginArea(innerRect);
            {
                GUILayout.Space(12f);
                VidaPremiumGUI.DrawBrandHeader(logoTexture, isCompact);
                GUILayout.Space(isCompact ? 12f : 18f);

                for (int i = 0; i < _items.Length; i++)
                {
                    ToolbarItem item = _items[i];
                    Rect itemRect = GUILayoutUtility.GetRect(innerRect.width, 42f, GUILayout.ExpandWidth(true), GUILayout.Height(42f));
                    if (VidaPremiumGUI.DrawSidebarItem(itemRect, item.Label, VidaPremiumGUI.GetPremiumTexture(item.IconName), i == GetSelectedIndex(), isCompact))
                    {
                        SetSelected(i);
                    }

                    GUILayout.Space(6f);
                }

                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(FrameworkUpdater.IsBusy || !VidaFramework.Connection))
                {
                    if (VidaPremiumGUI.DrawUpdateAction(innerRect.width, isCompact, FrameworkUpdater.IsBusy))
                        _ = FrameworkUpdater.CheckAsync();
                }
                GUILayout.Space(8f);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    VidaPremiumGUI.DrawConnectionStatus(VidaFramework.Connection, false, isCompact);
                    GUILayout.FlexibleSpace();
                }
            }
            GUILayout.EndArea();
        }

        public void DrawHeader(Rect headerRect)
        {
            VidaPremiumGUI.DrawHeaderBackground(headerRect);

            Rect innerRect = VidaPremiumGUI.GetInnerRect(headerRect, 24f);
            bool useIconActions = headerRect.width < 700f;
            GUILayout.BeginArea(innerRect);
            {
                using (new GUILayout.HorizontalScope())
                {
                    using (new GUILayout.VerticalScope(GUILayout.Width(innerRect.width - (useIconActions ? 120f : 302f))))
                    {
                        VidaPremiumGUI.DrawHeaderInfo(VidaFramework.Connection ? GetPageTitle() : "Welcome",
                            VidaFramework.Connection ? GetSelectedSubtitle() : "Connect your Vida account to get started.");
                    }

                    GUILayout.FlexibleSpace();

                    float cacheWidth = useIconActions ? 34f : 92f;
                    float reloadWidth = useIconActions ? 34f : 96f;
                    float logoutWidth = useIconActions ? 34f : 96f;

                    if (VidaPremiumGUI.DrawHeaderAction("Cache", VidaPremiumGUI.GetPremiumTexture("icon-cache-reset.png"), cacheWidth, false, false, useIconActions, true))
                    {
                        FrameworkStoreClient.ClearCache();
                        ResetPackageWindowData();
                        ReloadNeeded = true;
                    }

                    GUILayout.Space(6f);

                    if (VidaPremiumGUI.DrawHeaderAction("Reload", VidaPremiumGUI.GetPremiumTexture("icon-reload.png"), reloadWidth, false, false, useIconActions, true))
                    {
                        ReloadSelectedWindow();
                        ReloadNeeded = true;
                    }

                    if (VidaFramework.Connection)
                    {
                        GUILayout.Space(6f);
                        if (VidaPremiumGUI.DrawHeaderAction("Logout", VidaPremiumGUI.GetPremiumTexture("icon-logout.png"), logoutWidth, false, false, useIconActions, true))
                        {
                            Logout();
                        }
                    }
                }
            }
            GUILayout.EndArea();
        }
    
        public static bool ReloadNeeded
        {
            get => EditorPrefs.GetBool("MainToolbarNeedReload", false);
            set => EditorPrefs.SetBool("MainToolbarNeedReload", value);
        }
        
        
        public string GetSelected()
        {
            return _items[GetSelectedIndex()].Label;
        }
        public int GetSelectedIndex()
        {
            string selected = EditorPrefs.GetString("VidaFramework.SelectedPage", string.Empty);
            if (string.IsNullOrEmpty(selected))
            {
                // Preserve existing pages when the two empty tabs are removed.
                int legacyIndex = EditorPrefs.GetInt("MainToolbarSelectedIndex", 0);
                string[] legacyPages = { "Home", "Starter", "SDK", "Home", "Codes", "Home", "Packages" };
                selected = legacyIndex >= 0 && legacyIndex < legacyPages.Length ? legacyPages[legacyIndex] : "Home";
            }
            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i].Label == selected)
                {
                    return i;
                }
            }
            return 0;
        }

        private void SetSelected(int index)
        {
            EditorPrefs.SetString("VidaFramework.SelectedPage", _items[index].Label);
        }

        private string GetPageTitle()
        {
            switch (GetSelected())
            {
                case "Starter": return "Starter Packages";
                case "SDK": return "SDK Packages";
                case "Packages": return "Framework Packages";
                default: return GetSelected();
            }
        }

        private void ReloadSelectedWindow()
        {
            switch (GetSelected())
            {
                case "Starter":
                    StarterWindow.RequestReload();
                    break;
                case "SDK":
                    SdkWindow.RequestReload();
                    break;
                case "Codes":
                    global::Vida.Framework.CodesWindow.RequestReload();
                    break;
                case "Packages":
                    PackagesWindow.RequestReload();
                    break;
            }
        }

        private void ResetPackageWindowData()
        {
            StarterWindow.ResetCachedData();
            SdkWindow.ResetCachedData();
            TemplatesWindow.ResetCachedData();
            PackagesWindow.ResetCachedData();

            if (GetSelected() == "Codes")
            {
                global::Vida.Framework.CodesWindow.RequestReload();
            }
        }

        private async void Logout()
        {
            await FrameworkSession.SignOutAsync();
            FrameworkStoreClient.ClearCache();
            DataReader.CodeData.Clear();
        }

        private string GetSelectedSubtitle()
        {
            return _items[GetSelectedIndex()].Subtitle;
        }

        private readonly struct ToolbarItem
        {
            public readonly string Label;
            public readonly string IconName;
            public readonly string Subtitle;

            public ToolbarItem(string label, string iconName, string subtitle)
            {
                Label = label;
                IconName = iconName;
                Subtitle = subtitle;
            }
        }
    }
}
