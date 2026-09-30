using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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

        // The list is derived from the published Starter package. Hashes prevent exporting
        // unrelated or locally modified assets under that package's existing identity.
        public static void ExportAssetListForAutomation()
        {
            string listPath = GetCommandLineValue("-vidaPackageAssetList");
            string output = GetCommandLineValue("-vidaPackageOutput");
            if (string.IsNullOrWhiteSpace(listPath) || string.IsNullOrWhiteSpace(output))
                throw new ArgumentException("Provide -vidaPackageAssetList and -vidaPackageOutput.");
            PackageAssetList list = JsonUtility.FromJson<PackageAssetList>(File.ReadAllText(listPath));
            if (list?.assets == null || list.assets.Length == 0)
                throw new InvalidDataException("The package asset list is empty.");
            foreach (PackageAsset asset in list.assets)
            {
                if (asset == null || string.IsNullOrEmpty(asset.path)
                    || !asset.path.StartsWith("Assets/", StringComparison.Ordinal)
                    || asset.path.Contains("\\") || asset.path.Split('/').Any(part => part == ".." || part == "." || part.Length == 0))
                    throw new InvalidDataException("Package assets must use exact project-relative Assets paths.");
                // Native plugin bundle members can legitimately have no individual .meta.
                // Preserve that exact absence from the original archive too.
                if (string.IsNullOrEmpty(asset.metaSha256))
                {
                    if (asset.folder || File.Exists(asset.path + ".meta"))
                        throw new InvalidDataException("Package metadata differs from the selected artifact: " + asset.path);
                }
                else
                    CheckHash(asset.path + ".meta", asset.metaSha256);
                if (asset.folder)
                {
                    if (!AssetDatabase.IsValidFolder(asset.path))
                        throw new InvalidDataException("Package folder is missing: " + asset.path);
                }
                else
                    CheckHash(asset.path, asset.sha256);
            }
            string[] paths = list.assets.Select(asset => asset.path).ToArray();
            if (paths.Distinct(StringComparer.Ordinal).Count() != paths.Length)
                throw new InvalidDataException("Package asset paths must be unique.");
            ExportSigned(paths, Path.GetFullPath(output));
        }

        private static void Export(string output)
        {
            if (!AssetDatabase.IsValidFolder(FrameworkAssetPath))
            {
                throw new DirectoryNotFoundException("Framework assets were not found at " + FrameworkAssetPath + ".");
            }

            string[] assets = AssetDatabase.GetAllAssetPaths()
                .Where(path => path.StartsWith(FrameworkAssetPath + "/", StringComparison.Ordinal)
                    && !AssetDatabase.IsValidFolder(path)
                    && !path.StartsWith(FrameworkAssetPath + "/graphify-out/", StringComparison.Ordinal)
                    && !path.StartsWith(FrameworkAssetPath + "/Tests/", StringComparison.Ordinal)
                    && !path.EndsWith("/AGENTS.md", StringComparison.Ordinal)
                    && !path.EndsWith("/CODE_MAP.md", StringComparison.Ordinal))
                .ToArray();
            ExportSigned(assets, output);
        }

        private static void ExportSigned(string[] assets, string output)
        {
#if UNITY_6000_6_OR_NEWER
            string organizationId = GetCommandLineValue("-vidaSigningOrganization");
            if (string.IsNullOrWhiteSpace(organizationId))
                organizationId = CloudProjectSettings.organizationKey;
            if (string.IsNullOrWhiteSpace(organizationId))
                throw new InvalidOperationException("Signing needs a Unity organization. Link the intended organization or provide -vidaSigningOrganization <organization-id>; sign in to Unity as a member of that organization.");
            if (assets.Length == 0)
                throw new InvalidDataException("No assets were selected for export.");
            if (File.Exists(output))
                throw new IOException("Preserving the existing package. Choose a new output path: " + output);
            if (!output.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Signed asset packages must use the .unitypackage extension.");
            string directory = Path.GetDirectoryName(output);
            if (string.IsNullOrEmpty(directory))
                throw new InvalidOperationException("Package export output directory is invalid.");
            Directory.CreateDirectory(directory);
            UnityEditor.AssetPackage.Package.Export(new UnityEditor.AssetPackage.ExportPackageParameters(
                assets, output, organizationId, ExportPackageOptions.Default));

            FileInfo artifact = new FileInfo(output);
            if (!artifact.Exists || artifact.Length == 0)
            {
                throw new InvalidDataException("Unity did not create the signed package. Check Unity sign-in, organization membership and the export error.");
            }

            if (artifact.Length > MaximumStoreBytes)
            {
                throw new InvalidDataException(
                    $"Package is {artifact.Length} bytes and exceeds the {MaximumStoreBytes}-byte Store limit.");
            }

            // Unity can fall back to an unsigned export, and its native signing errors
            // are not reliably delivered through Application.logMessageReceived.
            if (!ContainsAttestation(output))
                throw new InvalidDataException("Unity produced an unsigned package. Do not publish this file as signed; check Editor sign-in and the organization's signing ID.");

            Debug.Log($"VIDA_FRAMEWORK_EXPORT path={artifact.FullName} bytes={artifact.Length} signingRequested=true signatureReadbackRequired=true");
#else
            throw new NotSupportedException("Signed .unitypackage export requires Unity 6.6 or later. Older Editors can still import the signed package.");
#endif
        }

        private static bool ContainsAttestation(string path)
        {
            // Verified against Unity 6000.6 SignedAssetPackage.AttestationFilename.
            // Presence is a minimum export gate, not cryptographic signature verification.
            using (FileStream file = File.OpenRead(path))
            using (GZipStream tar = new GZipStream(file, CompressionMode.Decompress))
            {
                byte[] block = new byte[512];
                byte[] discard = new byte[8192];
                while (ReadBlock(tar, block, block.Length))
                {
                    if (block.All(value => value == 0)) return false;
                    string name = Encoding.UTF8.GetString(block, 0, 100).TrimEnd('\0');
                    string prefix = Encoding.UTF8.GetString(block, 345, 155).TrimEnd('\0');
                    if (prefix.Length > 0) name = prefix + "/" + name;
                    long size = Convert.ToInt64(Encoding.ASCII.GetString(block, 124, 12).Trim('\0', ' '), 8);
                    if (name == "package/.attestation.p7m" && (block[156] == 0 || block[156] == (byte)'0'))
                        return size > 0 && size <= 1048576 && ReadBlock(tar, new byte[(int)size], (int)size);
                    long remaining = checked((size + 511) / 512 * 512);
                    while (remaining > 0)
                    {
                        int count = (int)Math.Min(remaining, discard.Length);
                        if (!ReadBlock(tar, discard, count)) throw new InvalidDataException("Incomplete exported package.");
                        remaining -= count;
                    }
                }
                return false;
            }
        }

        private static bool ReadBlock(Stream stream, byte[] buffer, int count)
        {
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read == 0) return false;
                offset += read;
            }
            return true;
        }

        private static void CheckHash(string path, string expected)
        {
            if (string.IsNullOrEmpty(expected) || !File.Exists(path))
                throw new InvalidDataException("Package source or expected SHA-256 is missing: " + path);
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                string actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Package source differs from the selected artifact: " + path);
            }
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

        [Serializable]
        private sealed class PackageAssetList
        {
            public PackageAsset[] assets;
        }

        [Serializable]
        private sealed class PackageAsset
        {
            public string path;
            public bool folder;
            public string sha256;
            public string metaSha256;
        }
    }
}
