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
            var window = GetWindow<VidaFramework>();
            Rect rect = window.position;
            rect.width = 980;
            rect.height = 600;
            
            float x = Screen.currentResolution.width / 2f - rect.width / 2;
            float y = Screen.currentResolution.height / 2f - rect.height / 2;
            rect.x = x;
            rect.y = y;
            
            window.position = rect;
            window.minSize = new Vector2(820, 440);
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
            Rect sidebarRect = new Rect(0f, 0f, VidaPremiumGUI.SidebarWidth, position.height);
            Rect headerRect = new Rect(sidebarRect.xMax + panelGap, 0f, position.width - sidebarRect.width - panelGap, VidaPremiumGUI.HeaderHeight);
            Rect contentRect = new Rect(headerRect.x, headerRect.yMax + panelGap, headerRect.width, position.height - headerRect.yMax - panelGap);

            _mainToolbar.DrawSidebar(sidebarRect, _backgroundTexture);
            _mainToolbar.DrawHeader(headerRect, _home.StartLogin);
            VidaPremiumGUI.DrawContentBackground(contentRect);

            Rect innerContentRect = VidaPremiumGUI.GetInnerRect(contentRect);
            GUILayout.BeginArea(innerContentRect);
            {
                DrawSelectedContent(innerContentRect.size);
            }
            GUILayout.EndArea();
        }

        private void LoadTextures()
        {
            _backgroundTexture = TextureLoader.GetTexture("vida-games-icon.png");
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
            if (!Connection)
            {
                _home.Draw();
                return;
            }

            switch (_mainToolbar.GetSelected())
            {
                case "Home":
                    _home.Draw();
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
