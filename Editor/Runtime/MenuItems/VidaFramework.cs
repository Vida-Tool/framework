#if UNITY_EDITOR
namespace Vida.Framework.Editor
{
    using System;
    using UnityEngine;
    using UnityEditor;
    
    public class VidaFramework : EditorWindow
    {
        public static bool Connection { get; set; } = false;
        public static bool AutoConnect { get; set; } = true;

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
        private static void OpenWindow()
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
            window.minSize = new Vector2(900, 500);
            window.titleContent = new GUIContent("Menu","Framework menu");
            
            VDefineSymbolInjector.Inject();
            
            Connection = GithubConnector.TryConnect();

            EditorApplication.update += EditorUpdate;
        }

        public static void EditorUpdate()
        {
            if(Connection && _activeLoginWindow != null)
            {
                GetWindow<VidaFramework>().TryCloseGitLoginWindow();
            }
            
        }
        private void OnDestroy()
        {
            EditorApplication.update -= EditorUpdate;
            if (_activeLoginWindow != null)
            {
                _activeLoginWindow.Close();
            }
        }


        private MainToolbar _mainToolbar = new MainToolbar();
        private HomeWindow _home = new HomeWindow();
        private TemplatesWindow _templates = new TemplatesWindow();
        private SettingsWindow _settings = new SettingsWindow();

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
            GithubConnector.TryConnect();
            
            _backgroundTexture = TextureLoader.GetTexture("vida-games-icon.png");
            
            if (Connection)
            {
                GithubConnector.ReadInfoFile(false);
                
            }
            
            TemplatesWindow.ResetEditorPrefs();
        }

        private void OnGUI()
        {
            var windowSize = position.size;
            windowSize.x -= 10;
            
            _mainToolbar.Draw(windowSize);
            GUILayout.Space(10);

            if (Connection)
            {
                TryCloseGitLoginWindow();
            }
            else
            {
                if (_activeLoginWindow == null)
                {
                    VGitLogin.ShowWindow(this);
                }
                else
                {
                    _activeLoginWindow.UpdatePosition(this);
                }

                return;
            }
            //

            if (_activeLoginWindow == null)
            {
                switch (_mainToolbar.GetSelected())
                {
                    case "Home":
                        _home.Draw();
                        break;
                    case "Templates":
                        _templates.Draw(windowSize);
                        break;
                    case "Settings":
                        _settings.Draw();
                        break;
                }
            }
            

            
            DrawBackgroundTexture();

        }

        private void TryCloseGitLoginWindow()
        {
            if(!AutoConnect) return;
            if (_activeLoginWindow != null)
            {
                _activeLoginWindow.Close();
            }
        }
    }
}
#endif
