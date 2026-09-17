using System;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public readonly struct PackageDisplayInfo
    {
        public PackageDisplayInfo(string category, string name, string version)
        {
            Category = string.IsNullOrEmpty(category) ? "uncategorized" : category;
            Name = name ?? string.Empty;
            Version = version ?? string.Empty;
        }

        public string Category { get; }
        public string Name { get; }
        public string Version { get; }
    }

    public static class StarterPackageInfoExtensions
    {
        public static void GetColumnWidths(float windowWidth, out float categoryWidth, out float nameWidth, out float versionWidth, out float downloadWidth)
        {
            float available = Mathf.Max(300f, windowWidth - 24f);
            bool showCategory = ShouldShowCategory(windowWidth);

            categoryWidth = showCategory ? Mathf.Clamp(available * 0.21f, 108f, 156f) : 0f;
            versionWidth = Mathf.Clamp(available * 0.14f, 72f, 104f);
            downloadWidth = Mathf.Clamp(available * 0.16f, 82f, 112f);
            nameWidth = Mathf.Max(120f, available - categoryWidth - versionWidth - downloadWidth);
        }

        public static bool ShouldShowCategory(float windowWidth)
        {
            return windowWidth >= 620f;
        }

        public static PackageDisplayInfo GetDisplayInfo(this StarterPackageInfo package)
        {
            return package == null
                ? new PackageDisplayInfo("uncategorized", string.Empty, string.Empty)
                : new PackageDisplayInfo(package.Kind, package.Name, package.DisplayVersion);
        }

        public static bool MatchesSearch(this StarterPackageInfo package, string searchText)
        {
            if (package == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(searchText))
            {
                return true;
            }

            PackageDisplayInfo info = package.GetDisplayInfo();
            return (!string.IsNullOrEmpty(info.Name)
                        && info.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                   || (!string.IsNullOrEmpty(info.Version)
                       && info.Version.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                   || (!string.IsNullOrEmpty(info.Category)
                       && info.Category.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
