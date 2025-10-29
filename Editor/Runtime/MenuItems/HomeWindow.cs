using System.Globalization;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public class HomeWindow
    {
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
                    GUILayout.Label("Starter paketleri artık Starter sekmesinden yönetilebilir.", EditorStyles.wordWrappedLabel);
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
    }
}
