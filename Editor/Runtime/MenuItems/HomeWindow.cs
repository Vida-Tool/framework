using System;
using System.Globalization;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public class HomeWindow
    {
        private string[] _label = new []{"...","..."};
        

        
        public void Draw()
        {
            GUILayout.Space(20);

            GitLogin();

            if (VidaFramework.Connection == false) return;
            
            
            GUILayout.Space(35);
            
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(20);
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal();
                    {
                        MenuWithItem("Download Starter", _label[1],() =>
                        {
                            if (VidaFramework.Connection)
                            {
                                GithubConnector.DownloadStarter((result) =>
                                {
                                    _label[1] = result ? "Success" : "Failed";
                                });
                            }
                            else
                            {
                                _ = GithubConnector.TryConnect(b =>
                                {
                                    VidaFramework.Connection = b;
                                });
                                
                                _label[1] = "Key is not valid";
                            }
                        });
                    }
                    GUILayout.EndHorizontal();
                } 
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        private void GitLogin()
        {
            GUILayout.Space(10);
            
            GUILayout.BeginVertical();
            GUILayout.Label("Please login to your GitHub account to continue.",VGUIStyle.CenteredLabel);
            GUILayout.Space(10);
            ApiKey = GUILayout.TextField(ApiKey, 128,GUILayout.Height(40));
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Try Connect",GUILayout.Height(40)))
                {
                    VidaFramework.AutoConnect = true;
                }
                
                if (GUILayout.Button("Login",GUILayout.Height(40)))
                {
                    _ = GithubConnector.TryConnect(b =>
                    {
                        VidaFramework.Connection = b;
                    });
                    
                    if (VidaFramework.Connection)
                    {
                        VidaFramework.AutoConnect = true;
                    }
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        private string ApiKey
        {
            get => GithubConnector.apiKey;
            set => GithubConnector.apiKey = value;
        }


        private void MenuWithItem(string buttonName,string label,Action buttonClick)
        {
            GUIStyle buttonStyle = new GUIStyle(GUIStyle.none);
            GUILayout.BeginHorizontal(buttonStyle, GUILayout.Width(200), GUILayout.Height(50));
            {
                GUILayout.BeginVertical();
                {
                    if (GUILayout.Button(buttonName, GUILayout.Width(150), GUILayout.Height(35)))
                    {
                        buttonClick.Invoke();
                    }

                    GUILayout.Label("Indirdikten sonra eklenecek kisimlar secilebilir.");
                }
                GUILayout.EndVertical();
            
                GUILayout.Space(25);
                        
                GUI.color = label == "Success" ? Color.green : (label == "Failed" ? Color.red : Color.white);
                GUILayout.Label(label, VGUIStyle.CenteredLabel, GUILayout.Height(35));
                GUI.color = Color.white;
            }
            GUILayout.EndHorizontal();
            
        }
        

    }
}