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
        
        public static GUILayoutOption ExpandHeight(bool expand = true)
        {
            GUILayoutOption result;
            if (expand)
            {
                result = GUILayout.ExpandHeight(true);
            }
            else
            {
                result = GUILayout.ExpandHeight(false);
            }
            
            return result;
        }
    }
}