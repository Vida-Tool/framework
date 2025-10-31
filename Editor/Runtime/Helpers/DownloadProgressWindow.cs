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

            Resolution resolution = Screen.currentResolution;
            float x = resolution.width / 2f - WindowWidth / 2f;
            float y = resolution.height / 2f - WindowHeight / 2f;
            window.position = new Rect(x, y, WindowWidth, WindowHeight);

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
            GUILayout.Space(18f);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(_message, EditorStyles.wordWrappedLabel, GUILayout.Width(WindowWidth - 40f));
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(20f);

            Rect progressRect = GUILayoutUtility.GetRect(WindowWidth - 40f, 24f);
            progressRect.x += 20f;
            progressRect.width -= 40f;

            float value = _isIndeterminate ? _animationValue : _progress;
            string label = _isIndeterminate ? " " : $"%{Mathf.RoundToInt(_progress * 100f)}";
            EditorGUI.ProgressBar(progressRect, value, label);
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
