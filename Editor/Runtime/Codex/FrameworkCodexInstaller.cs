using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    internal static class FrameworkCodexInstaller
    {
        private const string ResourcePath = "VidaFrameworkCodex/vida-framework-mcp.mjs";
        private const string ServerHeader = "[mcp_servers.vida-framework]";

        [MenuItem("Vida/Framework/Install or Repair Codex Bridge")]
        internal static void InstallInteractive()
        {
            bool accepted = EditorUtility.DisplayDialog(
                "Vida Framework Codex Bridge",
                "This installs the local Vida Framework MCP connector in your user profile and registers it in Codex. "
                + "It does not store Vida credentials or change the Unity project.",
                "Install or Repair",
                "Cancel");
            if (!accepted)
            {
                return;
            }

            try
            {
                Install();
                EditorUtility.DisplayDialog(
                    "Vida Framework Codex Bridge",
                    "The bridge is installed. Restart Codex, then open this Unity project before using the Vida Framework tools.",
                    "OK");
            }
            catch (Exception exception)
            {
                Debug.LogError("VIDA: Codex bridge installation failed. " + exception.Message);
                EditorUtility.DisplayDialog("Codex bridge installation failed", exception.Message, "OK");
            }
        }

        public static void ValidateForAutomation()
        {
            TextAsset connector = Resources.Load<TextAsset>(ResourcePath);
            if (connector == null || string.IsNullOrWhiteSpace(connector.text))
            {
                throw new InvalidDataException("The packaged Codex connector could not be loaded through Unity Resources.");
            }

            const string existing = "[mcp_servers.other]\nenabled = false\n";
            string configured = UpsertServerConfig(existing, "/tmp/node", "/tmp/vida-framework-mcp.mjs");
            if (!configured.Contains(ServerHeader)
                || !configured.Contains("command = \"/tmp/node\"")
                || !configured.Contains("args = [\"/tmp/vida-framework-mcp.mjs\"]")
                || !configured.StartsWith(existing, StringComparison.Ordinal)
                || !string.Equals(configured, UpsertServerConfig(configured, "/tmp/node", "/tmp/vida-framework-mcp.mjs"), StringComparison.Ordinal))
            {
                throw new InvalidDataException("The Codex MCP configuration update is not stable.");
            }

            Debug.Log("VIDA_CODEX_BRIDGE_VALIDATE bytes=" + connector.bytes.Length);
        }

        private static void Install()
        {
            TextAsset connector = Resources.Load<TextAsset>(ResourcePath);
            if (connector == null || string.IsNullOrWhiteSpace(connector.text))
            {
                throw new InvalidDataException("The Framework package does not contain the Codex connector.");
            }

            string nodePath = FindNodeExecutable();
            if (string.IsNullOrEmpty(nodePath))
            {
                throw new FileNotFoundException("Node.js was not found. Install Node.js 20 or later, restart Unity, and try again.");
            }

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(userProfile))
            {
                throw new DirectoryNotFoundException("The current user profile could not be resolved.");
            }

            string installDirectory = Path.Combine(userProfile, ".vida", "framework-mcp");
            string connectorPath = Path.Combine(installDirectory, "vida-framework-mcp.mjs");
            Directory.CreateDirectory(installDirectory);

            byte[] connectorBytes = new UTF8Encoding(false).GetBytes(connector.text);
            WriteBytesAtomic(connectorPath, connectorBytes);
            if (!FixedTimeEquals(Hash(connectorBytes), Hash(File.ReadAllBytes(connectorPath))))
            {
                throw new InvalidDataException("The installed Codex connector failed its integrity check.");
            }

            string configuredHome = Environment.GetEnvironmentVariable("CODEX_HOME");
            string codexHome = string.IsNullOrWhiteSpace(configuredHome)
                ? Path.Combine(userProfile, ".codex")
                : Path.GetFullPath(configuredHome);
            Directory.CreateDirectory(codexHome);
            string configPath = Path.Combine(codexHome, "config.toml");
            string existing = File.Exists(configPath) ? File.ReadAllText(configPath) : string.Empty;
            string updated = UpsertServerConfig(existing, nodePath, connectorPath);
            if (File.Exists(configPath) && !string.Equals(existing, updated, StringComparison.Ordinal))
            {
                File.Copy(configPath, configPath + ".vida-framework-backup", true);
            }

            WriteTextAtomic(configPath, updated);
        }

        internal static string UpsertServerConfig(string existing, string nodePath, string connectorPath)
        {
            string normalized = (existing ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            var lines = normalized.Split('\n').ToList();
            while (lines.Count > 0 && lines[lines.Count - 1].Length == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            var desired = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("command", QuoteToml(nodePath)),
                new KeyValuePair<string, string>("args", "[" + QuoteToml(connectorPath) + "]"),
                new KeyValuePair<string, string>("enabled", "true"),
                new KeyValuePair<string, string>("startup_timeout_sec", "10"),
                new KeyValuePair<string, string>("tool_timeout_sec", "60")
            };

            int start = lines.FindIndex(line => string.Equals(line.Trim(), ServerHeader, StringComparison.Ordinal));
            if (start < 0)
            {
                if (lines.Count > 0)
                {
                    lines.Add(string.Empty);
                }

                lines.Add(ServerHeader);
                lines.AddRange(desired.Select(item => item.Key + " = " + item.Value));
                return string.Join("\n", lines) + "\n";
            }

            int end = lines.FindIndex(start + 1, line => line.TrimStart().StartsWith("[", StringComparison.Ordinal));
            if (end < 0)
            {
                end = lines.Count;
            }

            foreach (KeyValuePair<string, string> item in desired)
            {
                int index = -1;
                for (int i = start + 1; i < end; i++)
                {
                    string trimmed = lines[i].TrimStart();
                    int separator = trimmed.IndexOf('=');
                    string key = separator < 0 ? string.Empty : trimmed.Substring(0, separator).Trim();
                    if (string.Equals(key, item.Key, StringComparison.Ordinal))
                    {
                        index = i;
                        break;
                    }
                }

                string value = item.Key + " = " + item.Value;
                if (index >= 0)
                {
                    lines[index] = value;
                }
                else
                {
                    lines.Insert(end, value);
                    end++;
                }
            }

            return string.Join("\n", lines) + "\n";
        }

        private static string FindNodeExecutable()
        {
            bool windows = Application.platform == RuntimePlatform.WindowsEditor;
            string executable = windows ? "node.exe" : "node";
            string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (string directory in path.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(directory))
                {
                    continue;
                }

                string candidate = Path.Combine(directory.Trim(), executable);
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }

            if (!windows)
            {
                string[] commonPaths = { "/opt/homebrew/bin/node", "/usr/local/bin/node", "/usr/bin/node" };
                return commonPaths.FirstOrDefault(File.Exists);
            }

            return null;
        }

        private static string QuoteToml(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private static void WriteTextAtomic(string path, string value)
        {
            WriteBytesAtomic(path, new UTF8Encoding(false).GetBytes(value));
        }

        private static void WriteBytesAtomic(string path, byte[] bytes)
        {
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllBytes(temporary, bytes);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(temporary, path);
        }

        private static byte[] Hash(byte[] bytes)
        {
            using SHA256 sha256 = SHA256.Create();
            return sha256.ComputeHash(bytes);
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            int difference = 0;
            for (int i = 0; i < left.Length; i++)
            {
                difference |= left[i] ^ right[i];
            }

            return difference == 0;
        }
    }
}
