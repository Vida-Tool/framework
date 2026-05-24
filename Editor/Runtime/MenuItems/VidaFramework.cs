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

        private static VGitLogin _activeLoginWindow
        {
            get
            {
                if (EditorWindow.HasOpenInstances<VGitLogin>())
                {
                    return GetWindow<VGitLogin>();
                }

                return null;
            }
        }

        [MenuItem("Vida/Menu")]
        internal static async void OpenWindow()
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

            if (!Connection)
            {
                bool result = await GithubConnector.TryConnectAsync();
                Connection = result;
            }
        }


        private void OnDestroy()
        {
            if (_activeLoginWindow != null)
            {
                _activeLoginWindow.Close();
            }
        }


        private MainToolbar _mainToolbar = new MainToolbar();
        private HomeWindow _home = new HomeWindow();
        private StarterWindow _starterWindow = new StarterWindow();
        private SdkWindow _sdkWindow = new SdkWindow();
        private TemplatesWindow _templates = new TemplatesWindow();
        private SettingsWindow _settings = new SettingsWindow();
        private CodesWindow _codesWindow = new CodesWindow();

        private Texture2D _backgroundTexture;

        private void OnEnable()
        {
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

            float outerPadding = VidaPremiumGUI.OuterPadding;
            Rect sidebarRect = new Rect(outerPadding, outerPadding, VidaPremiumGUI.SidebarWidth, position.height - outerPadding * 2f);
            Rect headerRect = new Rect(sidebarRect.xMax + outerPadding, outerPadding, position.width - sidebarRect.width - outerPadding * 3f, VidaPremiumGUI.HeaderHeight);
            Rect contentRect = new Rect(headerRect.x, headerRect.yMax + outerPadding, headerRect.width, position.height - headerRect.yMax - outerPadding * 2f);

            _mainToolbar.DrawSidebar(sidebarRect, _backgroundTexture);
            _mainToolbar.DrawHeader(headerRect);
            VidaPremiumGUI.DrawFrame(contentRect, "frame-panel.png");

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

        private void DrawSelectedContent(Vector2 contentSize)
        {
            if (!Connection)
            {
                _home.Draw();
                return;
            }

            if (_activeLoginWindow != null)
            {
                VidaPremiumGUI.DrawCenteredState(
                    "Login Window Active",
                    "GitHub login window is open. Complete or close it to continue.",
                    VidaPremiumGUI.GetPremiumTexture("icon-login.png"));
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
