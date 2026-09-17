using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public class HomeWindow
    {
        private readonly Action _repaint;
        private CancellationTokenSource _loginCancellation;
        private bool _isConnecting;
        private bool _isDisposed;
        private bool _hasLoginError;
        private string _loginStatus = "Sign in with your Vida account to access Framework packages.";

        public HomeWindow(Action repaint)
        {
            _repaint = repaint;
        }

        /// <summary>
        /// Arayüzün çizimini gerçekleştirir.
        /// </summary>
        public void Draw(Vector2 contentSize)
        {
            if (!VidaFramework.Connection)
            {
                GitLogin(contentSize);
                return;
            }

            VidaPremiumGUI.DrawCenteredState(
                "Connection Ready",
                "Browse Starter, SDK, Codes and Packages from the sidebar.",
                VidaPremiumGUI.GetPremiumTexture("status-connected.png"));
        }

        /// <summary>
        /// GitHub bağlantısı için giriş ekranını çizer.
        /// </summary>
        private void GitLogin(Vector2 contentSize)
        {
            const float pagePadding = 36f;
            float formWidth = Mathf.Max(320f, contentSize.x - pagePadding * 2f);

            VidaPremiumGUI.DrawContentBackground(new Rect(0f, 0f, contentSize.x, contentSize.y));
            GUILayout.Space(pagePadding);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(pagePadding);

                using (new GUILayout.VerticalScope(GUILayout.Width(formWidth)))
                {
                    VidaPremiumGUI.DrawSectionHeader("Vida Sign In", "Your browser will open for secure Vida authentication.");
                    VidaPremiumGUI.DrawInlineMessage(_loginStatus, _hasLoginError);
                    GUILayout.Space(10f);

                    using (new GUILayout.HorizontalScope())
                    {
                        if (_isConnecting)
                        {
                            if (VidaPremiumGUI.DrawHeaderAction("Cancel", VidaPremiumGUI.GetPremiumTexture("icon-logout.png"), 160f, false, true))
                            {
                                CancelLogin();
                            }
                        }
                        else
                        {
                            if (VidaPremiumGUI.DrawHeaderAction("Sign In", VidaPremiumGUI.GetPremiumTexture("icon-login.png"), 160f, true))
                            {
                                StartLogin();
                            }
                        }
                    }
                }

                GUILayout.FlexibleSpace();
            }

            GUILayout.FlexibleSpace();
        }

        /// <summary>
        /// Refreshes the window from the current Unity-session identity.
        /// </summary>
        public void RefreshSession()
        {
            Repaint();
        }

        /// <summary>
        /// Starts login attempts in the main window.
        /// </summary>
        private void StartLogin()
        {
            if (VidaFramework.Connection || _isConnecting)
            {
                return;
            }

            LoginAsync();
        }

        public void Dispose()
        {
            _isDisposed = true;
            _loginCancellation?.Cancel();
            _loginCancellation?.Dispose();
        }

        private async void LoginAsync()
        {
            if (!BeginLogin())
            {
                return;
            }

            CancellationToken cancellationToken = _loginCancellation.Token;

            try
            {
                SetLoginStatus("Waiting for Vida sign-in in your browser...", false);
                await FrameworkSession.SignInAsync(cancellationToken);
                CompleteLogin();
            }
            catch (OperationCanceledException)
            {
                SetLoginStatus("Connection canceled.", false);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("VIDA: Framework sign-in failed. " + exception.Message);
                SetLoginStatus("Sign-in failed. Check your Vida access and try again.", true);
            }
            finally
            {
                EndLogin();
            }
        }

        private bool BeginLogin()
        {
            if (_isDisposed || _isConnecting)
            {
                return false;
            }

            _loginCancellation?.Dispose();
            _loginCancellation = new CancellationTokenSource();
            _isConnecting = true;
            _hasLoginError = false;
            Repaint();
            return true;
        }

        private void EndLogin()
        {
            _isConnecting = false;
            Repaint();
        }

        private void CancelLogin(bool updateStatus = true)
        {
            if (!_isConnecting)
            {
                return;
            }

            _loginCancellation?.Cancel();
            if (updateStatus)
            {
                SetLoginStatus("Canceling connection...", false);
            }
        }

        private void CompleteLogin()
        {
            SetLoginStatus("Connected.", false);
        }

        private void SetLoginStatus(string status, bool hasError)
        {
            _loginStatus = status;
            _hasLoginError = hasError;
            Repaint();
        }

        private void Repaint()
        {
            if (!_isDisposed)
            {
                _repaint?.Invoke();
            }
        }
    }
}
