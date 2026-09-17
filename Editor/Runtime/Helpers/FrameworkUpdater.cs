using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Vida.Framework.Editor
{
    internal static class FrameworkUpdater
    {
        private const string FrameworkId = "vida-framework";
        internal static bool IsBusy { get; private set; }

        internal static async Task CheckAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            DownloadProgressWindow.Controller progress = null;
            try
            {
                if (!FrameworkSession.IsSignedIn)
                    throw new InvalidOperationException("Güncellemeleri kontrol etmek için Vida hesabına giriş yap.");
                if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Güncellemeden önce Play Mode'dan çık ve Unity işlemlerinin tamamlanmasını bekle.");
                if (UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(VidaFramework).Assembly) != null)
                    throw new InvalidOperationException("Bu kurulum Package Manager tarafından yönetiliyor. Framework'ü Package Manager üzerinden güncelle.");

                string installed = ReadInstalledVersion();
                progress = DownloadProgressWindow.Show("Framework Update", "Güncellemeler kontrol ediliyor…");
                FrameworkSession.SessionSnapshot session = FrameworkSession.Capture();
                var packages = await FrameworkStoreClient.GetPackagesAsync("framework", true);
                FrameworkSession.EnsureCurrent(session);
                StarterPackageInfo package = packages.SingleOrDefault(item => item.Id == FrameworkId);
                if (package == null)
                    throw new InvalidOperationException("Vida Framework bu hesabın paket kataloğunda bulunamadı.");
                if (!IsNewerVersion(package.Version, installed))
                {
                    progress.Close();
                    progress = null;
                    EditorUtility.DisplayDialog("Vida Framework", "Framework güncel. Kurulu sürüm: " + installed, "Tamam");
                    return;
                }
                if (!SupportsUnity(package.UnityMinVersion, Application.unityVersion))
                    throw new InvalidOperationException("Bu Framework sürümü Unity " + package.UnityMinVersion + " veya üstünü gerektiriyor.");
                // Check again after the catalog request, before replacing any assets.
                if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Unity şu anda meşgul. İşlem tamamlandıktan sonra tekrar dene.");
                FrameworkSession.EnsureCurrent(session);
                progress.SetMessage("Framework " + package.Version + " indiriliyor ve güncelleniyor…");
                await FrameworkStoreClient.DownloadAndImportAsync(package, progress, false);
            }
            catch (OperationCanceledException)
            {
                progress?.Close();
                progress = null;
                EditorUtility.DisplayDialog("Vida Framework", "Güncelleme iptal edildi. Oturumunu kontrol edip tekrar deneyebilirsin.", "Tamam");
            }
            catch (Exception exception)
            {
                progress?.Close();
                progress = null;
                Debug.LogWarning("VIDA: Framework update failed. " + exception.Message);
                EditorUtility.DisplayDialog("Güncelleme tamamlanamadı", exception.Message, "Tamam");
            }
            finally
            {
                progress?.Close();
                IsBusy = false;
                foreach (VidaFramework window in Resources.FindObjectsOfTypeAll<VidaFramework>()) window.Repaint();
            }
        }

        private static string ReadInstalledVersion()
        {
            string path = Path.Combine(Application.dataPath, "framework", "package.json");
            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
            if (manifest == null || manifest.name != "com.vida.framework" || string.IsNullOrWhiteSpace(manifest.version))
                throw new InvalidDataException("Kurulu Framework sürümü okunamadı.");
            return manifest.version;
        }

        internal static bool IsNewerVersion(string published, string installed)
        {
            if (!Version.TryParse(published, out Version next) || !Version.TryParse(installed, out Version current))
                throw new InvalidDataException("Framework sürüm numarası geçersiz; otomatik güncelleme durduruldu.");
            return next > current;
        }

        internal static bool SupportsUnity(string minimum, string current)
        {
            if (string.IsNullOrWhiteSpace(minimum)) return true;
            string required = Regex.Match(minimum, @"^\d+\.\d+(?:\.\d+)?").Value;
            string running = Regex.Match(current, @"^\d+\.\d+(?:\.\d+)?").Value;
            return Version.TryParse(required, out Version min) && Version.TryParse(running, out Version actual) && actual >= min;
        }

        [Serializable]
        private sealed class Manifest
        {
            public string name;
            public string version;
        }
    }
}
