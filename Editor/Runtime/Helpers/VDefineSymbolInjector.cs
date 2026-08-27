using UnityEditor;
using UnityEditor.Build;

namespace Vida.Framework.Editor
{
    public static class VDefineSymbolInjector
    {
        public static void Inject()
        {
            // Inject here
            // Add : "UNITASK_DOTWEEN_SUPPORT", "DOTWEEN_TEXTMESHPRO"

            Inject(NamedBuildTarget.Android);
            Inject(NamedBuildTarget.iOS);
            Inject(NamedBuildTarget.Standalone);

            AssetDatabase.Refresh();
        }

        private static void Inject(NamedBuildTarget buildTarget)
        {
            string symbols = PlayerSettings.GetScriptingDefineSymbols(buildTarget);

            bool changed = false;

            if (!symbols.Contains("UNITASK_DOTWEEN_SUPPORT"))
            {
                symbols += ";UNITASK_DOTWEEN_SUPPORT";
                changed = true;
            }
            if (!symbols.Contains("DOTWEEN_TEXTMESHPRO"))
            {
                symbols += ";DOTWEEN_TEXTMESHPRO";
                changed = true;
            }


            if (changed)
            {
                PlayerSettings.SetScriptingDefineSymbols(buildTarget, symbols);
            }
        }
    }
}
