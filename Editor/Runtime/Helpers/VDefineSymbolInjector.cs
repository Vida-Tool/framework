using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Vida.Editor
{
    public static class VDefineSymbolInjector
    {
        public static void Inject()
        {
            // Inject here
            // Add : "ODIN_INSPECTOR", "ODIN_INSPECTOR_3", "ODIN_INSPECTOR_3_1"
            
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            bool changed = false;
            
            if (!symbols.Contains("ODIN_INSPECTOR"))
            {
                symbols += ";ODIN_INSPECTOR";
                changed = true;
            }
            if (!symbols.Contains("ODIN_INSPECTOR_3"))
            {
                symbols += ";ODIN_INSPECTOR_3";
                changed = true;
            }
            if (!symbols.Contains("ODIN_INSPECTOR_3_1"))
            {
                symbols += ";ODIN_INSPECTOR_3_1";
                changed = true;
            }
            if (!symbols.Contains("UNITASK_DOTWEEN_SUPPORT"))
            {
                symbols += ";UNITASK_DOTWEEN_SUPPORT";
                changed = true;
            }
             

            if (changed)
            {
                Inject(symbols);
            }
            
            AssetDatabase.Refresh();
        }

        private static void Inject(string symbols)
        {
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android,symbols);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.iOS,symbols);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone,symbols);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Unknown,symbols);
        }
    }
}