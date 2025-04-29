using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public class VGitLogin : EditorWindow
    {
        public VidaFramework vidaFramework;
        private int autoLoginCount = 0;
        private const int maxAutoLoginAttempts = 10;
        private CancellationTokenSource autoLoginCancellation;
        private bool isLoggingIn = false;
        private string loginStatus = "Not connected.";

        /// <summary>
        /// VGitLogin penceresini gösterir ve gerekli ayarlamaları yapar.
        /// </summary>
        public static VGitLogin ShowWindow(VidaFramework vidaWindow)
        {
            if (vidaWindow == null)
            {
                vidaWindow = GetWindow<VidaFramework>();
            }

            VGitLogin activeWindow;
            if (EditorWindow.HasOpenInstances<VGitLogin>())
            {
                activeWindow = GetWindow<VGitLogin>();
                activeWindow.Focus();
                activeWindow.vidaFramework = vidaWindow;
                activeWindow.ResetAutoLogin();
            }
            else
            {
                activeWindow = CreateInstance<VGitLogin>();
                activeWindow.vidaFramework = vidaWindow;
                activeWindow.titleContent = new GUIContent("Vida Git Login");
                activeWindow.UpdatePosition(vidaWindow);
                activeWindow.ShowPopup();
                activeWindow.ResetAutoLogin();
                activeWindow.StartAutoLogin();
            }
            return activeWindow;
        }

        private void OnDestroy()
        {
            autoLoginCancellation?.Cancel();
        }

        /// <summary>
        /// Otomatik giriş işlemini başlatır.
        /// </summary>
        private async void StartAutoLogin()
        {
            if (isLoggingIn)
                return;

            isLoggingIn = true;
            autoLoginCancellation = new CancellationTokenSource();
            autoLoginCount = 0;
            loginStatus = "Attempting connection...";

            while (autoLoginCount < maxAutoLoginAttempts)
            {
                try
                {
                    bool result = await GithubConnector.TryConnectAsync();
                    VidaFramework.Connection = result;

                    if (result)
                    {
                        loginStatus = "Connection successful!";
                        VidaFramework.AutoConnect = true;
                        Close();
                        return;
                    }
                    else
                    {
                        loginStatus = $"Attempt {autoLoginCount + 1}: Connection failed. Retrying...";
                    }
                }
                catch (Exception ex)
                {
                    loginStatus = $"Error: {ex.Message}";
                }

                autoLoginCount++;
                try
                {
                    await Task.Delay(500, autoLoginCancellation.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
            loginStatus = "Max attempts reached. Please try again.";
            isLoggingIn = false;
        }

        /// <summary>
        /// Otomatik giriş denemesi durumunu sıfırlar.
        /// </summary>
        private void ResetAutoLogin()
        {
            autoLoginCancellation?.Cancel();
            autoLoginCount = 0;
            isLoggingIn = false;
            loginStatus = "Not connected.";
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.BeginVertical();
            GUILayout.Label("Please login to your GitHub account to continue.", VGUIStyle.CenteredLabel);
            GUILayout.Space(10);

            // API key giriş alanı
            ApiKey = GUILayout.TextField(ApiKey, 128, GUILayout.Height(50));
            GUILayout.Space(10);

            // Durum mesajı
            GUILayout.Label(loginStatus);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Close", GUILayout.Height(50)))
                {
                    VidaFramework.AutoConnect = true;
                    Close();
                }

                if (GUILayout.Button("Login", GUILayout.Height(50)))
                {
                    ResetAutoLogin();
                    StartAutoLogin();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Pencerenin pozisyonunu VidaFramework penceresine göre ayarlar.
        /// </summary>
        public void UpdatePosition(VidaFramework vidaWindow)
        {
            if (vidaWindow == null)
            {
                vidaWindow = this.vidaFramework;
                if (vidaWindow == null)
                {
                    return;
                }
            }
            Focus();
            this.position = new Rect(vidaWindow.position.x, vidaWindow.position.y, 
                                     vidaWindow.position.width, vidaWindow.position.height);
        }

        private string ApiKey
        {
            get => GithubConnector.ApiKey;
            set => GithubConnector.ApiKey = value;
        }
    }
}
