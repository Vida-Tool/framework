using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;
using VIDA.Editor;

namespace VidaFramework.Editor
{
    public class HomeWindow
    {
        public string apiKey
        {
            get => GithubConnector.apiKey;
            set => GithubConnector.apiKey = value;
        }
        public bool isConnectionChecked = false;
        public bool isConnectionSucceed = false;
        
        private string _label = "...";

        public void Draw(Vector2 windowSize)
        {
            if (isConnectionChecked == false && apiKey.Length > 0)
            {
                bool success = GithubConnector.TryConnect();
                _label = success ? "Success" : "Failed";
                isConnectionChecked = true;
                isConnectionSucceed = success;
                
                if (success)
                {
                    DownloadPackages();
                }
            }
            
            SirenixEditorGUI.BeginBox(GUILayout.Width(windowSize.x * 0.75f), GUILayout.Height(windowSize.y * 0.3f));
            {
                var newApiKey = SirenixEditorFields.TextField("", apiKey,GUILayout.Width(windowSize.x * 0.75f));
                if (newApiKey != apiKey)
                {
                    apiKey = newApiKey;
                    isConnectionChecked = false;
                }
                
                GUILayout.Space(10);
                
                GUIStyle buttonStyle = new GUIStyle(GUIStyle.none);
                
                GUILayout.BeginHorizontal(buttonStyle, GUILayout.Width(200), GUILayout.Height(50));
                {
                    GUILayout.Space(20);

                    if (GUILayout.Button("Check Key", GUILayout.Width(100)))
                    {
                        {
                            bool success = GithubConnector.TryConnect();
                            _label = success ? "Success" : "Failed";
                            isConnectionSucceed = success;
                            if (success)
                            {
                                DownloadPackages();
                            }
                        }
                    }
                    
                    GUILayout.Space(20);
                        
                    GUI.color = _label == "Success" ? Color.green : (_label == "Failed" ? Color.red : Color.white);
                    GUILayout.Label(_label);
                    GUI.color = Color.white;
                    
                }
                GUILayout.EndHorizontal();
                
                GUILayout.FlexibleSpace();
            }
            SirenixEditorGUI.EndBox();
            GUI.color = Color.white;
        }


        private static void DownloadPackages()
        {
            UnityInsidePackages.Install();
        }
    }
}