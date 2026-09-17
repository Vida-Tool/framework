using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Vida.Framework.Editor
{
    internal static class FrameworkStoreClient
    {
        private const string StoreBaseUrl = "https://packages.vida.games";
        private const string DownloadPath = "/api/download/";
        private const string ArtifactDownloadHost = "vida-packages-prod-artifactbucket-p4ksj3s6jd8m.s3.eu-central-1.amazonaws.com";
        private const string ArtifactPath = "/packages/";
        private const int PageSize = 100;

        private static CatalogCacheEntry _catalogCache;
        private static readonly Dictionary<string, PackageDetailsCacheEntry> PackageDetailsCache =
            new Dictionary<string, PackageDetailsCacheEntry>(StringComparer.Ordinal);

        public static async Task<List<StarterPackageInfo>> GetPackagesAsync(string kind, bool forceRefresh = false)
        {
            FrameworkSession.SessionSnapshot session = FrameworkSession.Capture();
            if (forceRefresh)
            {
                ClearCatalogCache();
            }

            CatalogCacheEntry entry = _catalogCache;
            if (entry == null || !entry.Matches(session))
            {
                entry = new CatalogCacheEntry(session, LoadCatalogAsync(session));
                _catalogCache = entry;
            }

            List<StarterPackageInfo> catalog;
            try
            {
                catalog = await entry.Task;
                FrameworkSession.EnsureCurrent(session);
            }
            catch
            {
                ClearCacheIfCurrent(entry);
                throw;
            }

            return catalog
                .Where(package => string.Equals(package.Kind, kind, StringComparison.Ordinal))
                .OrderBy(package => package.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static void ClearCache()
        {
            ClearCatalogCache();
            PackageDetailsCache.Clear();
        }

        private static void ClearCatalogCache()
        {
            _catalogCache = null;
        }

        private static void ClearCacheIfCurrent(CatalogCacheEntry entry)
        {
            if (ReferenceEquals(_catalogCache, entry))
            {
                _catalogCache = null;
            }
        }

        public static async Task DownloadAndImportAsync(StarterPackageInfo package, IProgress<float> progress = null, bool interactive = true)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            FrameworkSession.SessionSnapshot session = FrameworkSession.Capture();
            PackageDetails detail = await GetPackageDetailsAsync(package, session);
            FrameworkSession.EnsureCurrent(session);
            PackageVersionInfo version = detail.Versions.FirstOrDefault(item => item.Version == package.Version);
            if (version == null)
            {
                throw new InvalidOperationException("The published package version is no longer available.");
            }

            await DownloadAndImportAsync(package, version, session, progress, interactive);
        }

        public static Task<PackageDetails> GetPackageDetailsAsync(StarterPackageInfo package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            FrameworkSession.SessionSnapshot session = FrameworkSession.Capture();
            return GetPackageDetailsAsync(package, session);
        }

        private static async Task<PackageDetails> GetPackageDetailsAsync(
            StarterPackageInfo package,
            FrameworkSession.SessionSnapshot session)
        {
            if (!PackageDetailsCache.TryGetValue(package.Id, out PackageDetailsCacheEntry entry)
                || !entry.Matches(session, package))
            {
                entry = new PackageDetailsCacheEntry(session, package, LoadPackageDetailsAsync(package, session));
                PackageDetailsCache[package.Id] = entry;
            }

            try
            {
                PackageDetails details = await entry.Task;
                FrameworkSession.EnsureCurrent(session);
                return details;
            }
            catch
            {
                if (PackageDetailsCache.TryGetValue(package.Id, out PackageDetailsCacheEntry current)
                    && ReferenceEquals(current, entry))
                {
                    PackageDetailsCache.Remove(package.Id);
                }

                throw;
            }
        }

        private static async Task<PackageDetails> LoadPackageDetailsAsync(
            StarterPackageInfo package,
            FrameworkSession.SessionSnapshot session)
        {
            PackageDetailResponse response = await GetPackageDetailAsync(package.Id, session);
            FrameworkSession.EnsureCurrent(session);
            if (response.package.kind != package.Kind || response.package.tags == null)
            {
                throw new InvalidDataException("Framework package detail does not match the catalog.");
            }

            List<PackageVersionInfo> versions = new List<PackageVersionInfo>();
            foreach (PackageVersion version in response.versions)
            {
                ValidateArtifact(version.filename, version.size, version.sha256);
                if (string.IsNullOrWhiteSpace(version.version) || version.publishedAt <= 0)
                {
                    throw new InvalidDataException("Framework package detail contains an invalid published version.");
                }

                versions.Add(new PackageVersionInfo(
                    version.version,
                    version.filename,
                    version.size,
                    version.sha256,
                    version.releaseNotes,
                    version.publishedAt,
                    version.versionLabel));
            }

            if (!versions.Any(version => version.Version == package.Version))
            {
                throw new InvalidDataException("Framework package detail has no catalog latest version.");
            }

            return new PackageDetails(
                response.package.id,
                response.package.kind,
                response.package.name,
                response.package.summary,
                response.package.description,
                response.package.tags,
                response.package.iconUrl,
                response.package.unityMinVersion,
                response.package.updatedAt,
                versions);
        }

        public static async Task DownloadAndImportAsync(
            StarterPackageInfo package,
            PackageVersionInfo version,
            IProgress<float> progress = null)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            FrameworkSession.SessionSnapshot session = FrameworkSession.Capture();
            await DownloadAndImportAsync(package, version, session, progress);
        }

        private static async Task DownloadAndImportAsync(
            StarterPackageInfo package,
            PackageVersionInfo version,
            FrameworkSession.SessionSnapshot session,
            IProgress<float> progress,
            bool interactive = true)
        {
            ValidateArtifact(version.Filename, version.Size, version.Sha256);
            FrameworkSession.EnsureCurrent(session);
            string directory = null;

            try
            {
                DownloadTicket ticket = await CreateDownloadTicketAsync(package.Id, version.Version, session);
                FrameworkSession.EnsureCurrent(session);
                ValidateTicket(ticket, version);

                directory = Path.Combine("Temp", "VidaFrameworkDownloads", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(directory);
                string packagePath = Path.Combine(directory, ticket.filename);

                progress?.Report(0f);
                string location;
                using (UnityWebRequest request = CreateTicketDownloadRequest(ticket.url))
                {
                    UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                    while (!operation.isDone)
                    {
                        AbortIfSessionChanged(request, session);
                        await Task.Yield();
                    }

                    FrameworkSession.EnsureCurrent(session);
                    ValidateTicketRedirectStatus(request.responseCode);
                    location = request.GetResponseHeader("Location");
                }

                FrameworkSession.EnsureCurrent(session);
                Uri artifactUri = ValidateArtifactRedirect(
                    location,
                    ArtifactDownloadHost,
                    package.Id,
                    version.Version,
                    ticket.sha256);

                using (UnityWebRequest request = CreateArtifactDownloadRequest(artifactUri, packagePath))
                {
                    UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                    while (!operation.isDone)
                    {
                        AbortIfSessionChanged(request, session);
                        if (ticket.size > 0)
                        {
                            progress?.Report(Mathf.Clamp01((float)request.downloadedBytes / ticket.size));
                        }

                        await Task.Yield();
                    }

                    FrameworkSession.EnsureCurrent(session);
                    RejectAdditionalRedirect(request.responseCode);
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        throw CreateRequestException(request, "Package download failed");
                    }
                }

                FileInfo file = new FileInfo(packagePath);
                if (!file.Exists || file.Length != ticket.size)
                {
                    throw new InvalidDataException("Downloaded package size does not match the published artifact.");
                }

                string digest;
                using (FileStream stream = File.OpenRead(packagePath))
                using (SHA256 sha256 = SHA256.Create())
                {
                    digest = ToLowerHex(sha256.ComputeHash(stream));
                }

                FrameworkSession.EnsureCurrent(session);
                if (!FixedTimeEquals(digest, ticket.sha256))
                {
                    throw new InvalidDataException("Downloaded package checksum does not match the published artifact.");
                }

                progress?.Report(1f);
                await ImportPackageIfCurrentAsync(session, packagePath, interactive);
                if (!interactive) DeleteDownloadDirectory(directory);
            }
            catch
            {
                DeleteDownloadDirectory(directory);
                throw;
            }
        }

        private static void AbortIfSessionChanged(
            UnityWebRequest request,
            FrameworkSession.SessionSnapshot session)
        {
            if (FrameworkSession.IsCurrent(session))
            {
                return;
            }

            request.Abort();
            FrameworkSession.EnsureCurrent(session);
        }

        private static async Task ImportPackageIfCurrentAsync(
            FrameworkSession.SessionSnapshot session,
            string packagePath,
            bool interactive)
        {
            FrameworkSession.EnsureCurrent(session);
            if (interactive)
            {
                AssetDatabase.ImportPackage(packagePath, true);
                return;
            }

            var completion = new TaskCompletionSource<bool>();
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Unity is busy. Try the Framework update again when it is idle.");
            string expectedName = Path.GetFileNameWithoutExtension(packagePath);
            string expectedFilename = Path.GetFileName(packagePath);
            void Completed(string name)
            {
                if (name == expectedName || name == expectedFilename) completion.TrySetResult(true);
            }
            void Failed(string name, string error)
            {
                if (name == expectedName || name == expectedFilename)
                    completion.TrySetException(new InvalidOperationException(error));
            }
            void Cancelled(string name)
            {
                if (name == expectedName || name == expectedFilename) completion.TrySetCanceled();
            }
            AssetDatabase.importPackageCompleted += Completed;
            AssetDatabase.importPackageFailed += Failed;
            AssetDatabase.importPackageCancelled += Cancelled;
            try
            {
                FrameworkSession.EnsureCurrent(session);
                AssetDatabase.ImportPackage(packagePath, false);
                await completion.Task;
            }
            finally
            {
                AssetDatabase.importPackageCompleted -= Completed;
                AssetDatabase.importPackageFailed -= Failed;
                AssetDatabase.importPackageCancelled -= Cancelled;
            }
        }

        private static void DeleteDownloadDirectory(string directory)
        {
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        private static UnityWebRequest CreateTicketDownloadRequest(string url)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.redirectLimit = 0;
            request.timeout = 30;
            return request;
        }

        private static UnityWebRequest CreateArtifactDownloadRequest(Uri uri, string packagePath)
        {
            UnityWebRequest request = UnityWebRequest.Get(uri.AbsoluteUri);
            request.downloadHandler = new DownloadHandlerFile(packagePath);
            request.redirectLimit = 0;
            request.timeout = 300;
            return request;
        }

        private static void ValidateTicketRedirectStatus(long responseCode)
        {
            if (responseCode != 302 && responseCode != 307)
            {
                throw new InvalidDataException("Framework download ticket did not return an accepted redirect.");
            }
        }

        private static void RejectAdditionalRedirect(long responseCode)
        {
            if (responseCode >= 300 && responseCode < 400)
            {
                throw new InvalidDataException("Framework package download returned an additional redirect.");
            }
        }

        private static Uri ValidateArtifactRedirect(
            string location,
            string expectedHost,
            string packageId,
            string version,
            string sha256)
        {
            if (string.IsNullOrWhiteSpace(expectedHost)
                || string.IsNullOrWhiteSpace(location)
                || location.IndexOfAny(new[] { '\r', '\n', ',' }) >= 0
                || !Uri.TryCreate(location, UriKind.Absolute, out Uri uri))
            {
                throw new InvalidDataException("Framework package redirect is invalid.");
            }

            string expectedPath = ArtifactPath + packageId + "/" + version + "/" + sha256 + ".unitypackage";
            if (uri.Scheme != Uri.UriSchemeHttps
                || !string.IsNullOrEmpty(uri.UserInfo)
                || !string.Equals(uri.Host, expectedHost, StringComparison.OrdinalIgnoreCase)
                || !uri.IsDefaultPort
                || !string.Equals(uri.AbsolutePath, expectedPath, StringComparison.Ordinal)
                || !string.IsNullOrEmpty(uri.Fragment))
            {
                throw new InvalidDataException("Framework package redirect has an invalid destination.");
            }

            return uri;
        }

        private static async Task<List<StarterPackageInfo>> LoadCatalogAsync(
            FrameworkSession.SessionSnapshot session)
        {
            List<StarterPackageInfo> packages = new List<StarterPackageInfo>();
            HashSet<string> cursors = new HashSet<string>(StringComparer.Ordinal);
            string cursor = null;

            do
            {
                string url = StoreBaseUrl + "/api/catalog?limit=" + PageSize;
                if (!string.IsNullOrEmpty(cursor))
                {
                    url += "&cursor=" + UnityWebRequest.EscapeURL(cursor);
                }

                string json = await SendAuthorizedAsync(url, UnityWebRequest.kHttpVerbGET, session);
                FrameworkSession.EnsureCurrent(session);
                CatalogResponse response = JsonUtility.FromJson<CatalogResponse>(json);
                if (response?.items == null)
                {
                    throw new InvalidDataException("Framework catalog response is invalid.");
                }

                foreach (CatalogItem item in response.items)
                {
                    ValidateCatalogItem(item);
                    packages.Add(new StarterPackageInfo(
                        item.id,
                        item.kind,
                        item.name,
                        item.summary,
                        item.latestVersion,
                        item.iconUrl,
                        item.unityMinVersion,
                        item.updatedAt,
                        item.latestVersionLabel));
                }

                cursor = response.nextCursor;
                if (!string.IsNullOrEmpty(cursor) && !cursors.Add(cursor))
                {
                    throw new InvalidDataException("Framework catalog returned a repeated cursor.");
                }
            }
            while (!string.IsNullOrEmpty(cursor));

            return packages;
        }

        private static async Task<PackageDetailResponse> GetPackageDetailAsync(
            string packageId,
            FrameworkSession.SessionSnapshot session)
        {
            string url = StoreBaseUrl + "/api/packages/" + UnityWebRequest.EscapeURL(packageId);
            string json = await SendAuthorizedAsync(url, UnityWebRequest.kHttpVerbGET, session);
            FrameworkSession.EnsureCurrent(session);
            PackageDetailResponse response = JsonUtility.FromJson<PackageDetailResponse>(json);
            if (response?.package == null || response.versions == null || response.package.id != packageId)
            {
                throw new InvalidDataException("Framework package detail response is invalid.");
            }

            return response;
        }

        private static async Task<DownloadTicket> CreateDownloadTicketAsync(
            string packageId,
            string version,
            FrameworkSession.SessionSnapshot session)
        {
            string url = StoreBaseUrl
                         + "/api/packages/" + UnityWebRequest.EscapeURL(packageId)
                         + "/versions/" + UnityWebRequest.EscapeURL(version)
                         + "/download";
            string json = await SendAuthorizedAsync(url, UnityWebRequest.kHttpVerbPOST, session);
            FrameworkSession.EnsureCurrent(session);
            return JsonUtility.FromJson<DownloadTicket>(json);
        }

        private static async Task<string> SendAuthorizedAsync(
            string url,
            string method,
            FrameworkSession.SessionSnapshot session)
        {
            FrameworkSession.EnsureCurrent(session);

            using UnityWebRequest request = new UnityWebRequest(url, method);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 30;
            request.redirectLimit = 0;
            request.SetRequestHeader("Authorization", session.Authorization);
            if (method == UnityWebRequest.kHttpVerbPOST)
            {
                request.uploadHandler = new UploadHandlerRaw(Array.Empty<byte>());
            }

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                AbortIfSessionChanged(request, session);
                await Task.Yield();
            }

            FrameworkSession.EnsureCurrent(session);
            if (request.result != UnityWebRequest.Result.Success)
            {
                if (request.responseCode == 401)
                {
                    FrameworkSession.ClearIfCurrent(session);
                }

                throw CreateRequestException(request, "Framework Store request failed");
            }

            return request.downloadHandler.text;
        }

        private static Exception CreateRequestException(UnityWebRequest request, string prefix)
        {
            ErrorResponse response = null;
            if (!string.IsNullOrWhiteSpace(request.downloadHandler?.text))
            {
                response = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
            }

            string error = string.IsNullOrEmpty(response?.error) ? request.responseCode.ToString() : response.error;
            return new InvalidOperationException(prefix + ": " + error);
        }

        private static void ValidateCatalogItem(CatalogItem item)
        {
            if (item == null
                || string.IsNullOrWhiteSpace(item.id)
                || !IsKind(item.kind)
                || string.IsNullOrWhiteSpace(item.name)
                || string.IsNullOrWhiteSpace(item.latestVersion))
            {
                throw new InvalidDataException("Framework catalog contains an invalid package.");
            }
        }

        private static void ValidateTicket(DownloadTicket ticket, PackageVersionInfo version)
        {
            if (ticket == null || !Uri.TryCreate(ticket.url, UriKind.Absolute, out Uri uri))
            {
                throw new InvalidDataException("Framework download ticket is invalid.");
            }

            if (uri.Scheme != Uri.UriSchemeHttps
                || !string.IsNullOrEmpty(uri.UserInfo)
                || !string.Equals(uri.Host, "packages.vida.games", StringComparison.OrdinalIgnoreCase)
                || !uri.IsDefaultPort
                || !uri.AbsolutePath.StartsWith(DownloadPath, StringComparison.Ordinal)
                || uri.AbsolutePath.Length == DownloadPath.Length
                || uri.AbsolutePath.IndexOf('/', DownloadPath.Length) >= 0
                || !IsTicketToken(uri.AbsolutePath.Substring(DownloadPath.Length))
                || !string.IsNullOrEmpty(uri.Query)
                || !string.IsNullOrEmpty(uri.Fragment))
            {
                throw new InvalidDataException("Framework download ticket has an invalid destination.");
            }

            if (ticket.expiresAt <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                throw new InvalidDataException("Framework download ticket has expired.");
            }

            ValidateArtifact(ticket.filename, ticket.size, ticket.sha256);
            if (ticket.filename != version.Filename
                || ticket.size != version.Size
                || !FixedTimeEquals(ticket.sha256, version.Sha256))
            {
                throw new InvalidDataException("Framework download ticket does not match the published package.");
            }
        }

        private static void ValidateArtifact(string filename, long size, string sha256)
        {
            if (string.IsNullOrWhiteSpace(filename)
                || filename != Path.GetFileName(filename)
                || !filename.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase)
                || size <= 0
                || !IsLowerHex(sha256, 64))
            {
                throw new InvalidDataException("Framework package artifact metadata is invalid.");
            }
        }

        private static bool IsKind(string kind)
        {
            return kind == "framework" || kind == "starter" || kind == "sdk" || kind == "template" || kind == "package";
        }

        private static bool IsLowerHex(string value, int length)
        {
            if (value == null || value.Length != length)
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (!char.IsDigit(character) && (character < 'a' || character > 'f'))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsTicketToken(string value)
        {
            if (value == null || value.Length != 87 || value[43] != '.')
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (i == 43)
                {
                    continue;
                }

                char character = value[i];
                bool asciiLetterOrDigit = character >= 'a' && character <= 'z'
                                          || character >= 'A' && character <= 'Z'
                                          || character >= '0' && character <= '9';
                if (!asciiLetterOrDigit && character != '_' && character != '-')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            if (left == null || right == null || left.Length != right.Length)
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

        private static string ToLowerHex(byte[] bytes)
        {
            StringBuilder builder = new StringBuilder(bytes.Length * 2);
            foreach (byte value in bytes)
            {
                builder.Append(value.ToString("x2"));
            }

            return builder.ToString();
        }

        private sealed class CatalogCacheEntry
        {
            internal CatalogCacheEntry(
                FrameworkSession.SessionSnapshot session,
                Task<List<StarterPackageInfo>> task)
            {
                Generation = session.Generation;
                StudioId = session.StudioId;
                Task = task;
            }

            internal long Generation { get; }
            internal string StudioId { get; }
            internal Task<List<StarterPackageInfo>> Task { get; }

            internal bool Matches(FrameworkSession.SessionSnapshot session)
            {
                return Generation == session.Generation
                       && string.Equals(StudioId, session.StudioId, StringComparison.Ordinal);
            }
        }

        private sealed class PackageDetailsCacheEntry
        {
            internal PackageDetailsCacheEntry(
                FrameworkSession.SessionSnapshot session,
                StarterPackageInfo package,
                Task<PackageDetails> task)
            {
                Generation = session.Generation;
                StudioId = session.StudioId;
                PackageId = package.Id;
                Kind = package.Kind;
                Version = package.Version;
                UpdatedAt = package.UpdatedAt;
                Task = task;
            }

            internal long Generation { get; }
            internal string StudioId { get; }
            internal string PackageId { get; }
            internal string Kind { get; }
            internal string Version { get; }
            internal long UpdatedAt { get; }
            internal Task<PackageDetails> Task { get; }

            internal bool Matches(FrameworkSession.SessionSnapshot session, StarterPackageInfo package)
            {
                return Generation == session.Generation
                       && string.Equals(StudioId, session.StudioId, StringComparison.Ordinal)
                       && string.Equals(PackageId, package.Id, StringComparison.Ordinal)
                       && string.Equals(Kind, package.Kind, StringComparison.Ordinal)
                       && string.Equals(Version, package.Version, StringComparison.Ordinal)
                       && UpdatedAt == package.UpdatedAt;
            }
        }

        [Serializable]
        private sealed class CatalogResponse
        {
            public CatalogItem[] items;
            public string nextCursor;
        }

        [Serializable]
        private sealed class CatalogItem
        {
            public string id;
            public string kind;
            public string name;
            public string summary;
            public string latestVersion;
            public string latestVersionLabel;
            public string iconUrl;
            public string unityMinVersion;
            public long updatedAt;
        }

        [Serializable]
        private sealed class PackageDetailResponse
        {
            public PackageDetail package;
            public PackageVersion[] versions;
        }

        [Serializable]
        private sealed class PackageDetail
        {
            public string id;
            public string kind;
            public string name;
            public string summary;
            public string description;
            public string[] tags;
            public string iconUrl;
            public string unityMinVersion;
            public long updatedAt;
        }

        [Serializable]
        private sealed class PackageVersion
        {
            public string version;
            public string versionLabel;
            public string filename;
            public long size;
            public string sha256;
            public string releaseNotes;
            public long publishedAt;
        }

        [Serializable]
        private sealed class DownloadTicket
        {
            public string url;
            public long expiresAt;
            public string filename;
            public long size;
            public string sha256;
        }

        [Serializable]
        private sealed class ErrorResponse
        {
            public string error;
        }
    }

    public sealed class StarterPackageInfo
    {
        public StarterPackageInfo(
            string id,
            string kind,
            string name,
            string summary,
            string version,
            string iconUrl,
            string unityMinVersion,
            long updatedAt,
            string versionLabel = null)
        {
            Id = id;
            Kind = kind;
            Name = name;
            Summary = summary;
            Version = version;
            VersionLabel = versionLabel;
            IconUrl = iconUrl;
            UnityMinVersion = unityMinVersion;
            UpdatedAt = updatedAt;
        }

        public string Id { get; }
        public string Kind { get; }
        public string Name { get; }
        public string Summary { get; }
        public string Version { get; }
        public string VersionLabel { get; }
        public string DisplayVersion => string.IsNullOrWhiteSpace(VersionLabel) ? Version : VersionLabel;
        public string IconUrl { get; }
        public string UnityMinVersion { get; }
        public long UpdatedAt { get; }
    }

    public sealed class PackageDetails
    {
        public PackageDetails(
            string id,
            string kind,
            string name,
            string summary,
            string description,
            string[] tags,
            string iconUrl,
            string unityMinVersion,
            long updatedAt,
            IReadOnlyList<PackageVersionInfo> versions)
        {
            Id = id;
            Kind = kind;
            Name = name;
            Summary = summary;
            Description = description;
            Tags = tags;
            IconUrl = iconUrl;
            UnityMinVersion = unityMinVersion;
            UpdatedAt = updatedAt;
            Versions = versions;
        }

        public string Id { get; }
        public string Kind { get; }
        public string Name { get; }
        public string Summary { get; }
        public string Description { get; }
        public string[] Tags { get; }
        public string IconUrl { get; }
        public string UnityMinVersion { get; }
        public long UpdatedAt { get; }
        public IReadOnlyList<PackageVersionInfo> Versions { get; }
    }

    public sealed class PackageVersionInfo
    {
        public PackageVersionInfo(
            string version,
            string filename,
            long size,
            string sha256,
            string releaseNotes,
            long publishedAt,
            string versionLabel = null)
        {
            Version = version;
            VersionLabel = versionLabel;
            Filename = filename;
            Size = size;
            Sha256 = sha256;
            ReleaseNotes = releaseNotes;
            PublishedAt = publishedAt;
        }

        public string Version { get; }
        public string VersionLabel { get; }
        public string DisplayVersion => string.IsNullOrWhiteSpace(VersionLabel) ? Version : VersionLabel;
        public string Filename { get; }
        public long Size { get; }
        public string Sha256 { get; }
        public string ReleaseNotes { get; }
        public long PublishedAt { get; }
    }
}
