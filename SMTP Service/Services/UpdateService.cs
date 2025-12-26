using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Reflection;
using Serilog;

namespace SMTP_Service.Services
{
    /// <summary>
    /// Handles checking for updates and downloading from public GitHub releases.
    /// </summary>
    public class UpdateService
    {
        private readonly GitConfigurationManager _gitConfig;
        private readonly HttpClient _httpClient;
        private string? _downloadedFilePath;

        public UpdateService()
        {
            _gitConfig = new GitConfigurationManager();
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("SMTP-Service", GetCurrentVersion().ToString()));
        }

        public async Task<UpdateCheckResult> CheckForUpdateAsync()
        {
            try
            {
                if (!_gitConfig.IsConfigured)
                {
                    return new UpdateCheckResult
                    {
                        Available = false,
                        Error = "GitHub updates not configured. Check git.json configuration."
                    };
                }

                var url = $"https://api.github.com/repos/{_gitConfig.RepoOwner}/{_gitConfig.RepoName}/releases/latest";

                Log.Information("Checking for updates from GitHub releases");

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Warning($"GitHub API returned {response.StatusCode}");
                    return new UpdateCheckResult
                    {
                        Available = false,
                        Error = $"GitHub API error: {response.StatusCode}"
                    };
                }

                var json = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<GitHubRelease>(json);

                if (release == null)
                {
                    return new UpdateCheckResult { Available = false, Error = "Invalid release data" };
                }

                // Compare versions
                var currentVersion = GetCurrentVersion();
                var latestVersion = ParseVersion(release.tag_name);

                Log.Information($"Current version: {currentVersion}, Latest version: {latestVersion}");

                if (latestVersion > currentVersion)
                {
                    // Find the zip asset
                    var zipAsset = release.assets?.FirstOrDefault(a =>
                        a.name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true);

                    if (zipAsset == null)
                    {
                        Log.Warning("No zip file found in release assets");
                        return new UpdateCheckResult
                        {
                            Available = false,
                            Error = "No zip file found in release"
                        };
                    }

                    return new UpdateCheckResult
                    {
                        Available = true,
                        CurrentVersion = currentVersion.ToString(),
                        LatestVersion = latestVersion.ToString(),
                        DownloadUrl = zipAsset.browser_download_url,
                        FileName = zipAsset.name,
                        FileSize = zipAsset.size,
                        ReleaseNotes = release.body,
                        PublishedAt = release.published_at
                    };
                }

                return new UpdateCheckResult
                {
                    Available = false,
                    CurrentVersion = currentVersion.ToString(),
                    LatestVersion = currentVersion.ToString()
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to check for updates");
                return new UpdateCheckResult { Available = false, Error = ex.Message };
            }
        }

        public async Task<bool> DownloadUpdateAsync(string downloadUrl, string fileName, IProgress<int>? progress = null)
        {
            try
            {
                Log.Information($"Starting download from: {downloadUrl}");

                // Create updates folder
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var updateDir = Path.Combine(appDir, "updates");

                if (!Directory.Exists(updateDir))
                {
                    Directory.CreateDirectory(updateDir);
                    Log.Information($"Created update directory: {updateDir}");
                }

                // Clear old downloads
                foreach (var file in Directory.GetFiles(updateDir, "*.zip"))
                {
                    try
                    {
                        File.Delete(file);
                        Log.Information($"Deleted old update file: {file}");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, $"Could not delete old file: {file}");
                    }
                }

                var filePath = Path.Combine(updateDir, fileName);

                // Download directly from public GitHub URL
                Log.Information("Downloading from public GitHub release");

                using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var canReportProgress = totalBytes != -1 && progress != null;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                var totalRead = 0L;
                var read = 0;

                while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    totalRead += read;

                    if (canReportProgress)
                    {
                        var progressPercentage = (int)((totalRead * 100L) / totalBytes);
                        progress!.Report(progressPercentage);
                    }
                }

                Log.Information($"Download completed: {filePath} ({totalRead} bytes)");

                _downloadedFilePath = filePath;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to download update");
                return false;
            }
        }

        public string? GetDownloadedFilePath() => _downloadedFilePath;

        private Version GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
        }

        private Version ParseVersion(string? tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                return new Version(0, 0, 0);

            var versionString = tagName.TrimStart('v', 'V').TrimStart('.');

            if (Version.TryParse(versionString, out var version))
                return version;

            return new Version(0, 0, 0);
        }
    }

    public class UpdateCheckResult
    {
        public bool Available { get; set; }
        public string? CurrentVersion { get; set; }
        public string? LatestVersion { get; set; }
        public string? DownloadUrl { get; set; }
        public string? FileName { get; set; }
        public long FileSize { get; set; }
        public string? ReleaseNotes { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string? Error { get; set; }
    }

    public class GitHubRelease
    {
        public string? tag_name { get; set; }
        public string? name { get; set; }
        public string? body { get; set; }
        public DateTime published_at { get; set; }
        public GitHubAsset[]? assets { get; set; }
    }

    public class GitHubAsset
    {
        public long id { get; set; }
        public string? name { get; set; }
        public string? browser_download_url { get; set; }
        public long size { get; set; }
        public string? content_type { get; set; }
    }
}
