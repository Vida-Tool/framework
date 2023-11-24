

#if UNITY_EDITOR
namespace VidaFramework.Editor
{
    using System;
    using UnityEngine;
    using Sirenix.Utilities.Editor;
    using UnityEditor;
    using Sirenix.Utilities;
    
    public class VidaFramework : EditorWindow
    {
        [MenuItem("Vida/Window")]
        private static void OpenWindow()
        {
            var window = GetWindow<VidaFramework>();
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(800, 600);
            window.minSize = new Vector2(1000, 600);
        }


        private MainToolbar _mainToolbar = new MainToolbar();

        private HomeWindow _home = new HomeWindow();
        private TemplatesWindow _templates = new TemplatesWindow();

        private void DrawBackgroundTexture(Vector2 windowSize)
        {
            Texture2D texture2D = TextureLoader.GetTexture("vida-games.png");
            GUIStyle boxStyle = new GUIStyle(GUIStyle.none);
            boxStyle.normal.background = texture2D;
            //GUI.Box(new Rect(0,0,windowSize.x,windowSize.y), GUIContent.none, boxStyle);


            var width = texture2D.width * 0.5f;
            var height = texture2D.height * 0.5f;
            Rect textureRect = new Rect(windowSize.x/2f - width*0.5f,windowSize.y/2f - height*0.5f,width,height);
            // set texture with position to center
            GUI.DrawTexture(textureRect,texture2D);
        }

        private void OnGUI()
        {
            SirenixEditorGUI.Title("VIDA 0.1.0", "", TextAlignment.Center, true);
            GUILayout.Space(10);

            var windowSize = position.size;
            windowSize.x -= 10;
            

            
            DrawBackgroundTexture(windowSize);

            
            
            
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
                        default:
                            throw new ArgumentOutOfRangeException();
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
