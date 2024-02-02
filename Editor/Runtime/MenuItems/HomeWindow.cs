using System;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Vida.Framework.Editor
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
        
        private string[] _label = new []{"...","..."};

        public void Draw(Vector2 windowSize)
        {
            SirenixEditorGUI.BeginBox(GUILayout.Width(windowSize.x * 0.75f), GUILayout.Height(windowSize.y * 0.3f));
            {
                var newApiKey = SirenixEditorFields.TextField("", apiKey,GUILayout.Width(windowSize.x * 0.75f));
                if (newApiKey != apiKey)
                {
                    apiKey = newApiKey;
                    isConnectionChecked = false;
                }
                
                GUILayout.Space(15);


                GUILayout.BeginVertical();
                {
                    MenuWithItem("Check Key", _label[0],() =>
                    {
                        bool success = GithubConnector.TryConnect();
                        _label[0] = success ? "Success" : "Failed";
                        isConnectionSucceed = success;
                    });
                    MenuWithItem("Download Starter", _label[1],() =>
                    {
                        if (isConnectionSucceed )
                        {
                            GithubConnector.DownloadStarter((result) =>
                            {
                                _label[1] = result ? "Success" : "Failed";
                            });
                        }
                        else
                        {
                            _label[1] = "Key is not valid";
                        }
                    });
                } 
                GUILayout.EndVertical();
                
                
                GUILayout.FlexibleSpace();
            }
            SirenixEditorGUI.EndBox();
            GUI.color = Color.white;
        }


        private void MenuWithItem(string buttonName,string label,Action buttonClick)
        {
            GUIStyle buttonStyle = new GUIStyle(GUIStyle.none);
            GUILayout.BeginHorizontal(buttonStyle, GUILayout.Width(200), GUILayout.Height(50));
            {
                if (GUILayout.Button(buttonName, GUILayout.Width(150), GUILayout.Height(35)))
                {
                    buttonClick.Invoke();
                }
            
                GUILayout.Space(25);
                        
                GUI.color = label == "Success" ? Color.green : (label == "Failed" ? Color.red : Color.white);
                GUILayout.Label(label, VidaGUIStyles.CenteredLabel, GUILayout.Height(35));
                GUI.color = Color.white;
            }
            GUILayout.EndHorizontal();
        }
        
        
        public bool TryConnect()
        {
            isConnectionSucceed = GithubConnector.TryConnect();
            _label[0] = isConnectionSucceed ? "Success" : "Failed";
            return isConnectionSucceed;
        }
    }
}