using System;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public class SettingsWindow
    {
        private VGuiStyleSO Style => VGuiStyleSO.Style;

        public void Draw()
        {
            VidaPremiumGUI.DrawSectionHeader("Settings", "Framework style and local editor preferences.");
            VidaPremiumGUI.DrawCenteredState(
                "Settings Panel",
                "Style controls can be moved here after the premium window shell is approved.",
                VidaPremiumGUI.GetPremiumTexture("icon-settings.png"));
        }
    }
}
