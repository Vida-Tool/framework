#if UNITY_EDITOR
namespace Vida.Framework.Editor
{
    using UnityEngine;
    using UnityEditor;
    
    public class VidaFramework : EditorWindow
    {
        private const string ConnectionSessionKey = "VidaFramework.ConnectionApproved";
        private const string AutoConnectSessionKey = "VidaFramework.AutoConnect";

        public static bool Connection
        {
            get => SessionState.GetBool(ConnectionSessionKey, false);
            set => SessionState.SetBool(ConnectionSessionKey, value);
        }

        public static bool AutoConnect
        {
            get => SessionState.GetBool(AutoConnectSessionKey, true);
            set => SessionState.SetBool(AutoConnectSessionKey, value);
        }

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
            window.StartAutoConnect();
        }

        private void OnDestroy()
        {
            _home?.Dispose();
        }


        private MainToolbar _mainToolbar = new MainToolbar();
        private HomeWindow _home;
        private StarterWindow _starterWindow = new StarterWindow();
        private SdkWindow _sdkWindow = new SdkWindow();
        private TemplatesWindow _templates = new TemplatesWindow();
        private SettingsWindow _settings = new SettingsWindow();
        private CodesWindow _codesWindow = new CodesWindow();

        private Texture2D _backgroundTexture;

        private void OnEnable()
        {
            _home = new HomeWindow(Repaint);
            LoadTextures();
        }

        private void CreateGUI()
        {
            LoadTextures();
            TemplatesWindow.ResetCachedData();
        }

        private void OnGUI()
        {
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

        private void StartAutoConnect()
        {
            if (_home == null)
            {
                _home = new HomeWindow(Repaint);
            }

            _home.StartAutoConnect();
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
                case "Templates":
                    _templates.Draw(contentSize);
                    break;
                case "Settings":
                    _settings.Draw();
                    break;
                case "Codes":
                    _codesWindow.Draw(contentSize);
                    break;
            }
        }
    }
}
#endif
