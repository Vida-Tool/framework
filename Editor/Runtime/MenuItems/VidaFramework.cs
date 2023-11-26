

#if UNITY_EDITOR
namespace Vida.Editor
{
    using System;
    using UnityEngine;
    using Sirenix.Utilities.Editor;
    using UnityEditor;
    using Sirenix.Utilities;
    
    public class VidaFramework : EditorWindow
    {
        [MenuItem("Vida/Menu")]
        private static void OpenWindow()
        {
            VDefineSymbolInjector.Inject();

            var window = GetWindow<VidaFramework>();
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(900, 500);
            window.minSize = new Vector2(900, 500);
        }


        private MainToolbar _mainToolbar = new MainToolbar();
        private HomeWindow _home = new HomeWindow();
        private TemplatesWindow _templates = new TemplatesWindow();

        private Texture2D _backgroundTexture;
        
        private void DrawBackgroundTexture()
        {
            var windowSize = position.size;
            windowSize.x -= 25;
            windowSize.y -= 25;
            
            float width = _backgroundTexture.width * 0.3f;
            float height = _backgroundTexture.height * 0.3f;

            Rect textureRect = new Rect(windowSize.x - width,windowSize.y - height,width,height);
            GUI.DrawTexture(textureRect,_backgroundTexture);
        }

        private void CreateGUI()
        {
            _backgroundTexture = TextureLoader.GetTexture("vida-games.png");
            bool connected = _home.TryConnect();
            if (connected)
            {
                GithubConnector.ReadInfoFile(false);
            }
            
            TemplatesWindow.ResetEditorPrefs();
        }

        private void OnGUI()
        {
            var windowSize = position.size;
            windowSize.x -= 10;
            
            DrawBackgroundTexture();

            VidaEditorGUI.Title("VIDA", true);
            GUILayout.Space(10);
            
            SirenixEditorGUI.BeginBox(GUILayout.Width(windowSize.x), GUILayout.Height(windowSize.y - 20));
            {
                _mainToolbar.Draw(windowSize);
                GUILayout.Space(10);

                if (_home.isConnectionSucceed)
                {
                    switch (_mainToolbar.GetSelected())
                    {
                        case "Home":
                            _home.Draw(windowSize);
                            break;
                        case "Templates":
                            _templates.Draw(windowSize);
                            break;
                        case "Settings":
                            break;
                    }
                }
                else
                {
                    _home.Draw(windowSize);
                }
                

            }   
            SirenixEditorGUI.EndBox();
        }
    }
}
#endif
