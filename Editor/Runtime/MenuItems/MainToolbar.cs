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
            new ToolbarItem("Templates", "icon-templates.png", "Asset templates"),
            new ToolbarItem("Codes", "icon-codes.png", "Code snippets"),
            new ToolbarItem("Settings", "icon-settings.png", "Preferences")
        };

        public void DrawSidebar(Rect sidebarRect, Texture2D logoTexture)
        {
            VidaPremiumGUI.DrawSidebarBackground(sidebarRect);

            bool isCompact = sidebarRect.width < 100f;
            float padding = isCompact ? 10f : 12f;
            Rect innerRect = VidaPremiumGUI.GetInnerRect(sidebarRect, padding);
            GUILayout.BeginArea(innerRect);
            {
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

            Rect innerRect = VidaPremiumGUI.GetInnerRect(headerRect, 12f);
            bool useIconActions = headerRect.width < 720f;
            GUILayout.BeginArea(innerRect);
            {
                using (new GUILayout.HorizontalScope())
                {
                    using (new GUILayout.VerticalScope(GUILayout.Width(useIconActions ? 150f : 230f)))
                    {
                        VidaPremiumGUI.DrawHeaderInfo(GetSelected(), GetSelectedSubtitle());
                    }

                    GUILayout.FlexibleSpace();

                    float cacheWidth = useIconActions ? 34f : 92f;
                    float reloadWidth = useIconActions ? 34f : 96f;
                    float logoutWidth = useIconActions ? 34f : 96f;

                    if (VidaPremiumGUI.DrawHeaderAction("Cache", VidaPremiumGUI.GetPremiumTexture("icon-cache-reset.png"), cacheWidth, false, false, useIconActions))
                    {
                        GithubConnector.ClearUnityPackageCache(true);
                        ResetPackageWindowData();
                        ReloadNeeded = true;
                    }

                    GUILayout.Space(6f);

                    if (VidaPremiumGUI.DrawHeaderAction("Reload", VidaPremiumGUI.GetPremiumTexture("icon-reload.png"), reloadWidth, false, false, useIconActions))
                    {
                        ReloadSelectedWindow();
                        ReloadNeeded = true;
                    }

                    if (VidaFramework.Connection)
                    {
                        GUILayout.Space(6f);
                        if (VidaPremiumGUI.DrawHeaderAction("Logout", VidaPremiumGUI.GetPremiumTexture("icon-logout.png"), logoutWidth, false, true, useIconActions))
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
            int selectedIndex = EditorPrefs.GetInt("MainToolbarSelectedIndex", 0);
            if (selectedIndex < 0 || selectedIndex >= _items.Length)
            {
                return 0;
            }

            return selectedIndex;
        }
    
        private bool IsSelected(int index)
        {
            return EditorPrefs.GetInt("MainToolbarSelectedIndex", 0) == index;
        }
        private void SetSelected(int index)
        {
            EditorPrefs.SetInt("MainToolbarSelectedIndex", index);
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
                case "Templates":
                    TemplatesWindow.RequestReload();
                    break;
                case "Codes":
                    global::Vida.Framework.CodesWindow.RequestReload();
                    break;
            }
        }

        private void ResetPackageWindowData()
        {
            StarterWindow.ResetCachedData();
            SdkWindow.ResetCachedData();
            TemplatesWindow.ResetCachedData();

            if (GetSelected() == "Codes")
            {
                global::Vida.Framework.CodesWindow.RequestReload();
            }
        }

        private void Logout()
        {
            GithubConnector.ClearApiKey();
            GithubConnector.ResetConnection();
            VidaFramework.Connection = false;
            VidaFramework.AutoConnect = false;
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
