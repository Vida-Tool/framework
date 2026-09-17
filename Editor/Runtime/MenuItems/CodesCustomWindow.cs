using UnityEditor;
using UnityEngine;
using Vida.Framework.Editor;

namespace Vida.Framework
{
    public class CodesCustomWindow : EditorWindow
    {
        private readonly CodesWindow _codesWindow = new CodesWindow();

        [MenuItem("Vida/Codes", false, 0)]
        private static void OpenWindow()
        {
            bool wasOpen = HasOpenInstances<CodesCustomWindow>();
            CodesCustomWindow window = GetWindow<CodesCustomWindow>();
            window.minSize = new Vector2(720f, 480f);
            window.titleContent = new GUIContent("Codes", "Framework codes");
            if (!wasOpen)
            {
                Rect rect = new Rect(0f, 0f, 900f, 600f);
                rect.center = EditorGUIUtility.GetMainWindowPosition().center;
                window.position = rect;
            }
        }

        private void OnEnable() { wantsMouseMove = true; }

        private void OnGUI()
        {
            if (Event.current.type == EventType.MouseMove) Repaint();
            Rect rect = new Rect(0f, 0f, position.width, position.height);
            VidaPremiumGUI.DrawWindowBackground(rect);
            rect = VidaPremiumGUI.GetInnerRect(rect);
            GUILayout.BeginArea(rect);
            _codesWindow.Draw(rect.size, true);
            GUILayout.EndArea();
        }

        protected override void OnBackingScaleFactorChanged()
        {
            VidaPremiumGUI.ResetStyles();
            CodeEditorDrawer.Reset();
            Repaint();
        }
    }
}
