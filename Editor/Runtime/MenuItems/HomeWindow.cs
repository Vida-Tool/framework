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
            if (!VidaFramework.Connection)
            {
                GitLogin();
                return;
            }

            VidaPremiumGUI.DrawSectionHeader("Home", "Framework packages, templates and code helpers are ready.");
            VidaPremiumGUI.DrawCenteredState(
                "Connection Ready",
                "Starter, SDK and Templates tabs can now be managed from the left menu.",
                VidaPremiumGUI.GetPremiumTexture("status-connected.png"));
        }

        /// <summary>
        /// GitHub bağlantısı için giriş ekranını çizer.
        /// </summary>
        private void GitLogin()
        {
            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new GUILayout.VerticalScope(GUILayout.Width(440f)))
                {
                    Rect cardRect = GUILayoutUtility.GetRect(440f, 190f, GUILayout.Width(440f), GUILayout.Height(190f));
                    VidaPremiumGUI.DrawFrame(cardRect, "frame-panel-selected.png");

                    GUILayout.BeginArea(VidaPremiumGUI.GetInnerRect(cardRect, 18f));
                    {
                        VidaPremiumGUI.DrawSectionHeader("GitHub Connection", "Connect once to load Vida framework packages.");
                        ApiKey = VidaPremiumGUI.DrawSearchField(ApiKey, 404f);
                        GUILayout.Space(14f);

                        using (new GUILayout.HorizontalScope())
                        {
                            if (VidaPremiumGUI.DrawHeaderAction("Try", VidaPremiumGUI.GetPremiumTexture("icon-reload.png"), 128f))
                            {
                                TryConnectAsync();
                            }

                            GUILayout.Space(8f);

                            if (VidaPremiumGUI.DrawHeaderAction("Login", VidaPremiumGUI.GetPremiumTexture("icon-login.png"), 128f, true))
                            {
                                LoginAsync();
                            }
                        }
                    }
                    GUILayout.EndArea();
                }

                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();
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
