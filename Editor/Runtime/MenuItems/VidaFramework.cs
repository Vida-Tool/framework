#if UNITY_EDITOR
namespace Vida.Framework.Editor
{
    using UnityEngine;
    using UnityEditor;
    
    public class VidaFramework : EditorWindow
    {
        public static bool Connection => FrameworkSession.IsSignedIn;

        [MenuItem("Vida/Menu")]
        internal static void OpenWindow()
        {
            bool wasOpen = HasOpenInstances<VidaFramework>();
            var window = GetWindow<VidaFramework>();
            window.minSize = new Vector2(720f, 480f);

            if (!wasOpen)
            {
                Rect mainWindowRect = EditorGUIUtility.GetMainWindowPosition();
                Rect rect = new Rect(0f, 0f, 980f, 600f);
                rect.center = mainWindowRect.center;
                window.position = rect;
            }

            window.titleContent = new GUIContent("Vida Framework","Framework menu");
            
            VDefineSymbolInjector.Inject();
            window.RefreshSession();
        }

        private void OnDestroy()
        {
            _home?.Dispose();
        }


        private MainToolbar _mainToolbar = new MainToolbar();
        private HomeWindow _home;
        private StarterWindow _starterWindow = new StarterWindow();
        private SdkWindow _sdkWindow = new SdkWindow();
        private PackagesWindow _packages = new PackagesWindow();
        private CodesWindow _codesWindow = new CodesWindow();

        private Texture2D _backgroundTexture;
        private long _sessionGeneration;

        private void OnEnable()
        {
            FrameworkSession.SessionChanged -= HandleSessionChanged;
            FrameworkSession.SessionChanged += HandleSessionChanged;
            _sessionGeneration = FrameworkSession.Generation;
            _home = new HomeWindow(Repaint);
            wantsMouseMove = true;
            LoadTextures();
        }

        private void OnDisable()
        {
            FrameworkSession.SessionChanged -= HandleSessionChanged;
        }

        private void CreateGUI()
        {
            LoadTextures();
            TemplatesWindow.ResetCachedData();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.MouseMove) Repaint();
            RefreshSessionGeneration();
            Rect windowRect = new Rect(0f, 0f, position.width, position.height);
            VidaPremiumGUI.DrawWindowBackground(windowRect);

            float panelGap = VidaPremiumGUI.PanelGap;
            float sidebarWidth = VidaPremiumGUI.GetSidebarWidth(position.width);
            Rect sidebarRect = new Rect(0f, 0f, sidebarWidth, position.height);
            Rect headerRect = new Rect(sidebarRect.xMax + panelGap, 0f, position.width - sidebarRect.width - panelGap, VidaPremiumGUI.HeaderHeight);
            Rect contentRect = new Rect(headerRect.x, headerRect.yMax + panelGap, headerRect.width, position.height - headerRect.yMax - panelGap);

            _mainToolbar.DrawSidebar(sidebarRect, _backgroundTexture);
            _mainToolbar.DrawHeader(headerRect);
            VidaPremiumGUI.DrawContentBackground(contentRect);

            if (!Connection)
            {
                GUILayout.BeginArea(contentRect);
                {
                    _home.Draw(contentRect.size);
                }
                GUILayout.EndArea();
                return;
            }

            Rect innerContentRect = VidaPremiumGUI.GetInnerRect(contentRect);
            GUILayout.BeginArea(innerContentRect);
            {
                DrawSelectedContent(innerContentRect.size);
            }
            GUILayout.EndArea();
        }

        private void LoadTextures()
        {
            _backgroundTexture = TextureLoader.GetTexture("vida-hub-icon.png");
        }

        protected override void OnBackingScaleFactorChanged()
        {
            VidaPremiumGUI.ResetStyles();
            global::Vida.Framework.CodeEditorDrawer.Reset();
            Repaint();
        }

        private void RefreshSession()
        {
            if (_home == null)
            {
                _home = new HomeWindow(Repaint);
            }

            _home.RefreshSession();
        }

        private void RefreshSessionGeneration()
        {
            if (_sessionGeneration != FrameworkSession.Generation)
            {
                HandleSessionChanged();
            }
        }

        private void HandleSessionChanged()
        {
            _sessionGeneration = FrameworkSession.Generation;
            FrameworkStoreClient.ClearCache();
            StarterWindow.ResetCachedData();
            SdkWindow.ResetCachedData();
            TemplatesWindow.ResetCachedData();
            PackagesWindow.ResetCachedData();
            PackageDetailsWindow.CloseForSessionChange();
            _home?.RefreshSession();
            Repaint();
        }

        private void DrawSelectedContent(Vector2 contentSize)
        {
            switch (_mainToolbar.GetSelected())
            {
                case "Home":
                    _home.Draw(contentSize);
                    break;
                case "Starter":
                    _starterWindow.Draw(contentSize);
                    break;
                case "SDK":
                    _sdkWindow.Draw(contentSize);
                    break;
                case "Codes":
                    _codesWindow.Draw(contentSize);
                    break;
                case "Packages":
                    _packages.Draw(contentSize);
                    break;
            }
        }
    }
}
#endif
