using System;
using System.IO;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public readonly struct PackageDisplayInfo
    {
        public PackageDisplayInfo(string category, string name, string version)
        {
            Category = string.IsNullOrEmpty(category) ? "none" : category;
            Name = name ?? string.Empty;
            Version = version ?? string.Empty;
        }

        public string Category { get; }
        public string Name { get; }
        public string Version { get; }
    }

    public static class StarterPackageInfoExtensions
    {
        public static void GetColumnWidths(float windowWidth, out float categoryWidth, out float nameWidth, out float versionWidth, out float dateWidth, out float downloadWidth)
        {
            float available = Mathf.Max(300f, windowWidth - 40f);
            categoryWidth = available * 0.18f;
            nameWidth = available * 0.32f;
            versionWidth = available * 0.15f;
            dateWidth = available * 0.25f;
            downloadWidth = available * 0.10f;
        }

        public static PackageDisplayInfo GetDisplayInfo(this StarterPackageInfo package)
        {
            return ParseDisplayInfo(package?.Name);
        }

        public static PackageDisplayInfo ParseDisplayInfo(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return new PackageDisplayInfo("none", string.Empty, string.Empty);
            }

            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrEmpty(nameWithoutExtension))
            {
                return new PackageDisplayInfo("none", string.Empty, string.Empty);
            }

            string[] parts = nameWithoutExtension.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length <= 0)
            {
                return new PackageDisplayInfo("none", nameWithoutExtension, string.Empty);
            }

            if (parts.Length == 1)
            {
                return new PackageDisplayInfo("none", parts[0], string.Empty);
            }

            if (parts.Length == 2)
            {
                string first = parts[0];
                string second = parts[1];
                if (second.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                {
                    return new PackageDisplayInfo("none", first, second);
                }

                return new PackageDisplayInfo(first, second, string.Empty);
            }

            string category = parts[0];
            string packageName = parts[1];
            string version = parts[^1];
            return new PackageDisplayInfo(category, packageName, version);
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
                       && info.Category.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                   || (package.LastUpdated.HasValue
                       && package.LastUpdated.Value.ToString("dd.MM.yyyy HH:mm")
                           .IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
