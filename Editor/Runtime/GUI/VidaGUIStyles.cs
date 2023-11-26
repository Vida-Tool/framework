using UnityEngine;

namespace Vida.Editor
{
    public static class VidaGUIStyles
    {
        public static readonly Color LightBorderColor = (Color) new Color32((byte) 90, (byte) 90, (byte) 90, byte.MaxValue);
        
        
        public static GUIStyle CenteredLabel
        {
            get
            {
                GUIStyle guiStyle = new GUIStyle(GUI.skin.label);
                guiStyle.alignment = TextAnchor.MiddleCenter;
                return guiStyle;
            }
        }
    }
}