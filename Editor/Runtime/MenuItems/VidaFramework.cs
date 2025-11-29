#if UNITY_EDITOR
namespace Vida.Framework.Editor
{
    using System;
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

        private static int _loadIconIndex = 0;
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
        private static async void OpenWindow()
        {
            var window = GetWindow<VidaFramework>();
            Rect rect = window.position;
            rect.width = 900;
            rect.height = 500;
            
            float x = Screen.currentResolution.width / 2f - rect.width / 2;
            float y = Screen.currentResolution.height / 2f - rect.height / 2;
            rect.x = x;
            rect.y = y;
            
            window.position = rect;
            window.minSize = new Vector2(700, 300);
            window.titleContent = new GUIContent("Menu","Framework menu");
            
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
        
        private void DrawBackgroundTexture()
        {
            var windowSize = position.size;
            windowSize.x -= 25;
            windowSize.y -= 25;

            windowSize.x += _backgroundTexture.width * 0.07f;
            windowSize.y += _backgroundTexture.height * 0.15f;
            
            float width = _backgroundTexture.width * 0.3f;
            float height = _backgroundTexture.height * 0.4f;

            Rect textureRect = new Rect(windowSize.x - width,windowSize.y - height,width,height);
            GUI.DrawTexture(textureRect,_backgroundTexture);
        }

        private void CreateGUI()
        {
            
            _backgroundTexture = TextureLoader.GetTexture("vida-games-icon.png");
            
            TemplatesWindow.ResetCachedData();
        }

        private void OnGUI()
        {
            var windowSize = position.size;
            windowSize.x -= 10;
            
            _mainToolbar.Draw(windowSize);
            GUILayout.Space(10);

            if (!Connection)
            {
                _home.Draw();
                return;
            }

            if (_activeLoginWindow == null)
            {
                switch (_mainToolbar.GetSelected())
                {
                    case "Home":
                        _home.Draw();
                        break;
                    case "Starter":
                        _starterWindow.Draw(windowSize);
                        break;
                    case "SDK":
                        _sdkWindow.Draw(windowSize);
                        break;
                    case "Templates":
                        _templates.Draw(windowSize);
                        break;
                    case "Settings":
                        _settings.Draw();
                        break;
                    case "Codes":
                        _codesWindow.Draw(windowSize);
                        return;
                }
            }
            

            
            DrawBackgroundTexture();

        }
    }
}
#endif
