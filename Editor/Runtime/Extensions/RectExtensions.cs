using UnityEngine;

namespace Vida.Editor
{
    public static class RectExtensions
    {
        public static Rect AlignBottom(this Rect rect, float height)
        {
            rect.y = rect.y + rect.height - height;
            rect.height = height;
            return rect;
        }
    }
}