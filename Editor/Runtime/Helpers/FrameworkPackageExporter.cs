using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    public static class FrameworkPackageExporter
    {
        private const string FrameworkAssetPath = "Assets/framework";
        private const long MaximumStoreBytes = 100_000_000L;

        [MenuItem("Vida/Framework/Export Bootstrap Package")]
        public static void ExportInteractive()
        {
            string version = ReadVersion();
            string output = EditorUtility.SaveFilePanel(
                "Export Vida Framework",
                Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath,
                "Vida-Framework-" + version,
                "unitypackage");
            if (!string.IsNullOrEmpty(output))
            {
                Export(output);
            }
        }

        public static void ExportForAutomation()
        {
            string output = GetCommandLineValue("-vidaFrameworkOutput");
            if (string.IsNullOrWhiteSpace(output))
            {
                string root = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
                output = Path.Combine(root, "BuildArtifacts", "Vida-Framework-" + ReadVersion() + ".unitypackage");
            }

            Export(Path.GetFullPath(output));
        }

        private static void Export(string output)
        {
            if (!AssetDatabase.IsValidFolder(FrameworkAssetPath))
            {
                throw new DirectoryNotFoundException("Framework assets were not found at " + FrameworkAssetPath + ".");
            }

            string directory = Path.GetDirectoryName(output);
            if (string.IsNullOrEmpty(directory))
            {
                throw new InvalidOperationException("Framework export output directory is invalid.");
            }

            Directory.CreateDirectory(directory);
            string[] assets = AssetDatabase.GetAllAssetPaths()
                .Where(path => path.StartsWith(FrameworkAssetPath + "/", StringComparison.Ordinal)
                    && !AssetDatabase.IsValidFolder(path)
                    && !path.StartsWith(FrameworkAssetPath + "/graphify-out/", StringComparison.Ordinal)
                    && !path.EndsWith("/AGENTS.md", StringComparison.Ordinal)
                    && !path.EndsWith("/CODE_MAP.md", StringComparison.Ordinal))
                .ToArray();
            AssetDatabase.ExportPackage(assets, output, ExportPackageOptions.Default);

            FileInfo artifact = new FileInfo(output);
            if (!artifact.Exists || artifact.Length == 0)
            {
                throw new InvalidDataException("Unity did not create the Framework bootstrap package.");
            }

            if (artifact.Length > MaximumStoreBytes)
            {
                throw new InvalidDataException(
                    $"Framework bootstrap is {artifact.Length} bytes and exceeds the {MaximumStoreBytes}-byte Store limit.");
            }

            Debug.Log($"VIDA_FRAMEWORK_EXPORT path={artifact.FullName} bytes={artifact.Length}");
        }

        private static string ReadVersion()
        {
            string manifestPath = Path.Combine(Application.dataPath, "framework", "package.json");
            PackageManifest manifest = JsonUtility.FromJson<PackageManifest>(File.ReadAllText(manifestPath));
            if (manifest == null || string.IsNullOrWhiteSpace(manifest.version))
            {
                throw new InvalidDataException("Framework package.json has no version.");
            }

            return manifest.version;
        }

        private static string GetCommandLineValue(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] == name)
                {
                    return arguments[i + 1];
                }
            }

            return null;
        }

        [Serializable]
        private sealed class PackageManifest
        {
            public string version;
        }
    }
}
