using System;
using System.Globalization;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public class HomeWindow
    {
        private string[] _label = new[] { "...", "..." };

        /// <summary>
        /// Arayüzün çizimini gerçekleştirir.
        /// </summary>
        public void Draw()
        {
            GUILayout.Space(20);

            GitLogin();

            if (!VidaFramework.Connection)
                return;

            GUILayout.Space(35);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(20);
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal();
                    {
                        MenuWithItem("Download Starter", _label[1], async () =>
                        {
                            if (VidaFramework.Connection)
                            {
                                bool result = await GithubConnector.DownloadStarterAsync();
                                _label[1] = result ? "Success" : "Failed";
                            }
                            else
                            {
                                bool result = await GithubConnector.TryConnectAsync();
                                VidaFramework.Connection = result;
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

        /// <summary>
        /// GitHub bağlantısı için giriş ekranını çizer.
        /// </summary>
        private void GitLogin()
        {
            GUILayout.Space(10);

            GUILayout.BeginVertical();
            GUILayout.Label("Please login to your GitHub account to continue.", VGUIStyle.CenteredLabel);
            GUILayout.Space(10);
            ApiKey = GUILayout.TextField(ApiKey, 128, GUILayout.Height(40));
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Try Connect", GUILayout.Height(40)))
                {
                    TryConnectAsync();
                }

                if (GUILayout.Button("Login", GUILayout.Height(40)))
                {
                    LoginAsync();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Asenkron olarak bağlantı denemesi yapar.
        /// </summary>
        private async void TryConnectAsync()
        {
            bool result = await GithubConnector.TryConnectAsync();
            VidaFramework.Connection = result;
        }

        /// <summary>
        /// Asenkron olarak giriş işlemini gerçekleştirir; başarılı ise AutoConnect bayrağını ayarlar.
        /// </summary>
        private async void LoginAsync()
        {
            bool result = await GithubConnector.TryConnectAsync();
            VidaFramework.Connection = result;
            if (result)
            {
                VidaFramework.AutoConnect = true;
            }
        }

        private string ApiKey
        {
            get => GithubConnector.ApiKey;
            set => GithubConnector.ApiKey = value;
        }

        /// <summary>
        /// Belirtilen isimdeki buton ve yanında durum bilgisini gösterir.
        /// </summary>
        /// <param name="buttonName">Buton üzerinde görünecek metin.</param>
        /// <param name="label">Butonun yanındaki durum bilgisi.</param>
        /// <param name="buttonClick">Butona tıklandığında çalışacak metot.</param>
        private void MenuWithItem(string buttonName, string label, Action buttonClick)
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

                    GUILayout.Label("İndirildikten sonra eklenecek kısımlar seçilebilir.");
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
