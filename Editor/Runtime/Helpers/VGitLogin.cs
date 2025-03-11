using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public class VGitLogin : EditorWindow
    {
        public VidaFramework _vidaFramework;
        private static int autoLoginCount = 0;
        public static VGitLogin ShowWindow(VidaFramework vidaWindow)
        {
            if(vidaWindow == null)
            {
                vidaWindow = GetWindow<VidaFramework>();
            }
            
            VGitLogin _activeWindow;
            if(EditorWindow.HasOpenInstances<VGitLogin>())
            {
                _activeWindow = GetWindow<VGitLogin>();
                // öne al
                _activeWindow.Focus();
                _activeWindow._vidaFramework = vidaWindow;
                //_activeWindow.UpdatePosition(vidaWindow);
                autoLoginCount = 0;
                return _activeWindow;
            }
            else
            {
                _activeWindow = CreateInstance<VGitLogin>();
                _activeWindow._vidaFramework = vidaWindow;
                _activeWindow.titleContent = new GUIContent("Vida Git Login");
                _activeWindow.UpdatePosition(vidaWindow);
                _activeWindow.ShowPopup();
                autoLoginCount = 0;
                // on editor update
                EditorApplication.update += MUpdate;
                return _activeWindow;
            }
        }

        private void OnDestroy()
        {
            EditorApplication.update -= MUpdate;
        }

        private static void MUpdate()
        {
            if (EditorWindow.HasOpenInstances<VGitLogin>())
            {
                //GetWindow<VGitLogin>().UpdatePosition(null);
            }

            if (autoLoginCount < 10)
            {
                _ = GithubConnector.TryConnect(b =>
                {
                    VidaFramework.Connection = b;
                });
                
                autoLoginCount++;
            }
        }
        
        private void OnGUI()
        {

            if (!focusedWindow)
            {
               // Focus();
            }
            
            GUILayout.Space(10);
            
            GUILayout.BeginVertical();
            GUILayout.Label("Please login to your GitHub account to continue.",VGUIStyle.CenteredLabel);
            GUILayout.Space(10);
            ApiKey = GUILayout.TextField(ApiKey, 128,GUILayout.Height(50));
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Close",GUILayout.Height(50)))
                {
                    VidaFramework.AutoConnect = true;
                    Close();
                }
                
                if (GUILayout.Button("Login",GUILayout.Height(50)))
                {
                    _ = GithubConnector.TryConnect(b =>
                    {
                        VidaFramework.Connection = b;
                    });
                    
                    if (VidaFramework.Connection)
                    {
                        VidaFramework.AutoConnect = true;
                        Close();
                    }
                }
            
  
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        public void UpdatePosition(VidaFramework vidaFramework)
        {
            if (vidaFramework == null)
            {
                vidaFramework = _vidaFramework;
                if (vidaFramework == null)
                {
                    return;
                }
            }
            Focus();
            this.position = new Rect(vidaFramework.position.x, vidaFramework.position.y, vidaFramework.position.width, vidaFramework.position.height);
        }
        
        
        private string ApiKey
        {
            get => GithubConnector.apiKey;
            set => GithubConnector.apiKey = value;
        }
    }
}