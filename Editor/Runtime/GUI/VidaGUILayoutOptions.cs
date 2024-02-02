using UnityEngine;

namespace Vida.Framework.Editor
{
    public static class VidaGUILayoutOptions
    {
        public static GUILayoutOption ExpandWidth(bool expand = true)
        {
            GUILayoutOption result;
            if (expand)
            {
                result = GUILayout.ExpandWidth(true);
            }
            else
            {
                result = GUILayout.ExpandWidth(false);
            }
            
            return result;
        }
    }
}