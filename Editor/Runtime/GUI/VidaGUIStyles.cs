using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Vida.Editor
{
    public static class VidaGUIStyles
    {
        public static readonly Color LightBorderColor = (Color) new Color32((byte) 90, (byte) 90, (byte) 90, byte.MaxValue);
        
        public static readonly Color LightGray = new Color(211f / 255f, 211f / 255f, 211f / 255f); // R: 211, G: 211, B: 211
        public static readonly Color LightBlue = new Color(173f / 255f, 216f / 255f, 230f / 255f); // R: 173, G: 216, B: 230
        public static readonly Color SoftGreen = new Color(152f / 255f, 251f / 255f, 152f / 255f); // R: 152, G: 251, B: 152
        public static readonly Color PastelYellow = new Color(255f / 255f, 255f / 255f, 153f / 255f); // R: 255, G: 255, B: 153
        public static readonly Color MatteGray = new Color(128f / 255f, 128f / 255f, 128f / 255f);

        public static readonly Color DefaultColor = new Color(1, 1, 1);
        
        public static GUIStyle CenteredLabel
        {
            get
            {
                GUIStyle guiStyle = new GUIStyle(GUI.skin.label);
                guiStyle.alignment = TextAnchor.MiddleCenter;
                return guiStyle;
            }
        }
        
        private static GUIStyle toolbarButton;
        private static GUIStyle toolbarButtonSelected;
        
        public static GUIStyle ToolbarButton
        {
            get
            {
                if (toolbarButton == null)
                    toolbarButton = new GUIStyle(EditorStyles.toolbarButton)
                    {
                        fixedHeight = 0.0f,
                        alignment = TextAnchor.MiddleCenter,
                        stretchHeight = true,
                        stretchWidth = false,
                        margin = new RectOffset(0, 0, 0, 0)
                    };
                return toolbarButton;
            }
        }

        /// <summary>Toolbar button selected style.</summary>
        public static GUIStyle ToolbarButtonSelected
        {
            get
            {
                if (toolbarButtonSelected == null)
                    toolbarButtonSelected = new GUIStyle(SirenixGUIStyles.ToolbarButton)
                    {
                        normal = new GUIStyle(SirenixGUIStyles.ToolbarButton).onNormal
                    };
                return toolbarButtonSelected;
            }
        }
    }
}