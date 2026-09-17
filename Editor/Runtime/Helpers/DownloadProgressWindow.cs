using System;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    /// <summary>
    /// Modüler bir indirme ilerleme penceresi.
    /// </summary>
    public sealed class DownloadProgressWindow : EditorWindow
    {
        private const float AnimationSpeed = 0.6f;
        private const float WindowWidth = 360f;
        private const float WindowHeight = 130f;

        private string _message;
        private bool _isIndeterminate = true;
        private float _progress;
        private float _animationValue;
        private double _lastUpdateTime;

        /// <summary>
        /// Yeni bir indirme ilerleme penceresi oluşturur.
        /// </summary>
        public static Controller Show(string title, string message)
        {
            DownloadProgressWindow window = CreateInstance<DownloadProgressWindow>();
            window.titleContent = new GUIContent(title);
            window._message = message;
            window.minSize = new Vector2(WindowWidth, WindowHeight);
            window.maxSize = new Vector2(WindowWidth, WindowHeight);

            Rect rect = new Rect(0f, 0f, WindowWidth, WindowHeight);
            rect.center = EditorGUIUtility.GetMainWindowPosition().center;
            window.position = rect;

            window.ShowUtility();
            window.Focus();
            return new Controller(window);
        }

        private void OnEnable()
        {
            _lastUpdateTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            double currentTime = EditorApplication.timeSinceStartup;
            double delta = currentTime - _lastUpdateTime;
            _lastUpdateTime = currentTime;

            if (_isIndeterminate)
            {
                _animationValue = Mathf.Repeat(_animationValue + (float)(delta * AnimationSpeed), 1f);
            }

            Repaint();
        }

        private void OnGUI()
        {
            VidaPremiumGUI.DrawWindowBackground(new Rect(0f, 0f, position.width, position.height));
            GUILayout.BeginArea(new Rect(24f, 20f, position.width - 48f, position.height - 40f));
            VidaPremiumGUI.DrawBodyText(_message);
            GUILayout.FlexibleSpace();
            Rect track = GUILayoutUtility.GetRect(1f, 6f, GUILayout.ExpandWidth(true));
            VidaPremiumGUI.DrawRoundedRect(track, new Color32(0x35, 0x32, 0x3A, 0xFF), 3f);
            Rect fill = track;
            if (_isIndeterminate)
            {
                fill.width *= 0.24f;
                fill.x += (track.width - fill.width) * (0.5f - 0.5f * Mathf.Cos(_animationValue * Mathf.PI * 2f));
            }
            else
            {
                fill.width *= _progress;
            }
            VidaPremiumGUI.DrawRoundedRect(fill, new Color32(0xA5, 0x92, 0xDA, 0xFF), 3f);
            GUILayout.Space(10f);
            VidaPremiumGUI.DrawBodyText(_isIndeterminate ? "Please wait…" : Mathf.RoundToInt(_progress * 100f) + "%", true);
            GUILayout.EndArea();
        }

        private void SetProgress(float progress)
        {
            _progress = Mathf.Clamp01(progress);
            _isIndeterminate = false;
        }

        private void SetIndeterminate()
        {
            _isIndeterminate = true;
        }

        private void SetMessage(string message)
        {
            _message = message;
        }

        private void CloseWindow()
        {
            DownloadProgressWindow window = this;
            EditorApplication.delayCall += () =>
            {
                if (window != null)
                {
                    window.Close();
                }
            };
        }

        /// <summary>
        /// Pencereyi yönetmek için kullanılan yardımcı sınıf.
        /// </summary>
        public sealed class Controller : IProgress<float>, IDisposable
        {
            private DownloadProgressWindow _window;

            internal Controller(DownloadProgressWindow window)
            {
                _window = window;
            }

            /// <summary>
            /// İlerleme değerini günceller.
            /// </summary>
            public void Report(float value)
            {
                _window?.SetProgress(value);
            }

            /// <summary>
            /// Mesaj içeriğini günceller.
            /// </summary>
            public void SetMessage(string message)
            {
                _window?.SetMessage(message);
            }

            /// <summary>
            /// Belirsiz ilerleme moduna geçer.
            /// </summary>
            public void SetIndeterminate()
            {
                _window?.SetIndeterminate();
            }

            /// <summary>
            /// Pencereyi kapatır.
            /// </summary>
            public void Close()
            {
                if (_window != null)
                {
                    _window.CloseWindow();
                    _window = null;
                }
            }

            public void Dispose()
            {
                Close();
            }
        }
    }
}
