using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using SMTPServiceUpdater.Models;

namespace SMTPServiceUpdater.Services
{
    /// <summary>
    /// Handles checking for and downloading updates from public GitHub releases.
    /// </summary>
    public class GitHubDownloader
    {
        // Default public repository - used as fallback if git.json is missing or invalid
        private const string DefaultRepoOwner = "Kingofthecarts";
        private const string DefaultRepoName = "SMTP-To-MSGraph-Service";

        private readonly UpdateLogger _logger;
        private readonly string _rootPath;
        private static readonly HttpClient _httpClient = new HttpClient();

        public GitHubDownloader(UpdateLogger logger, string rootPath)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Root path cannot be null or empty", nameof(rootPath));
            }

            _rootPath = rootPath;

            // Configure HttpClient (only once)
            if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"SMTPServiceUpdater/{AppVersion.Version}");
            }
        }

        /// <summary>
        /// Checks GitHub for the latest release and compares with current version.
        /// </summary>
        public async Task<GitHubRelease?> CheckForUpdateAsync()
        {
            try
            {
                _logger.WriteLog("Connecting to GitHub...", LogLevel.Info);

                // Read git.json configuration, fall back to defaults if missing or invalid
                GitHubConfig? config = ReadGitConfig();
                string owner = config?.GitHub?.RepoOwner ?? "";
                string repository = config?.GitHub?.RepoName ?? "";

                // Check if values are missing, empty, or appear to be encrypted (contain = or /)
                bool useDefaults = string.IsNullOrWhiteSpace(owner) ||
                                   string.IsNullOrWhiteSpace(repository) ||
                                   owner.Contains('=') || owner.Contains('/') ||
                                   repository.Contains('=') || repository.Contains('/');

                if (useDefaults)
                {
                    owner = DefaultRepoOwner;
                    repository = DefaultRepoName;
                    _logger.WriteLog("Using default repository (git.json missing or invalid)", LogLevel.Info);
                }

                _logger.WriteLog($"Repository: {owner}/{repository}", LogLevel.Info);

                // Get current version
                string currentVersion = VersionManager.GetCurrentVersion(_rootPath);
                _logger.WriteLog($"Current installed version: {currentVersion}", LogLevel.Info);

                // Build GitHub API URL
                string apiUrl = $"https://api.github.com/repos/{owner}/{repository}/releases/latest";
                _logger.WriteLog("Querying GitHub API...", LogLevel.Info);

                // Call GitHub API (no authentication needed for public repos)
                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.WriteLog($"GitHub API error: {response.StatusCode}", LogLevel.Error);
                    return null;
                }

                _logger.WriteLog("Successfully connected to GitHub", LogLevel.Success);

                // Parse response
                string jsonResponse = await response.Content.ReadAsStringAsync();
                GitHubRelease? release = JsonSerializer.Deserialize<GitHubRelease>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (release == null)
                {
                    _logger.WriteLog("Failed to parse GitHub release information", LogLevel.Error);
                    return null;
                }

                _logger.WriteLog($"Latest GitHub version: {release.Version}", LogLevel.Success);

                // Log available assets
                if (release.Assets.Count > 0)
                {
                    _logger.WriteLog($"Found {release.Assets.Count} asset(s) in release:", LogLevel.Info);
                    foreach (var asset in release.Assets)
                    {
                        _logger.WriteLog($"  - {asset.Name} ({asset.Size / 1024:N0} KB)", LogLevel.Info);
                    }
                }

                // Compare versions
                if (VersionInfo.TryParse(release.Version, out VersionInfo? latestVersion) &&
                    VersionInfo.TryParse(currentVersion, out VersionInfo? currentVersionInfo))
                {
                    _logger.WriteLog($"Version comparison: {currentVersion} vs {release.Version}", LogLevel.Info);

                    if (latestVersion > currentVersionInfo)
                    {
                        _logger.WriteLog($"UPDATE AVAILABLE: New version {release.Version} is available", LogLevel.Success);
                        return release;
                    }
                    else if (latestVersion == currentVersionInfo)
                    {
                        _logger.WriteLog($"You are running the latest version ({currentVersion})", LogLevel.Success);
                        return null;
                    }
                    else
                    {
                        _logger.WriteLog($"Current version ({currentVersion}) is newer than GitHub ({release.Version})", LogLevel.Warning);
                        return null;
                    }
                }

                _logger.WriteLog("Could not compare versions", LogLevel.Warning);
                return null;
            }
            catch (HttpRequestException ex)
            {
                _logger.WriteLog($"Network error: {ex.Message}", LogLevel.Error);
                return null;
            }
            catch (Exception ex)
            {
                _logger.WriteLog($"Error checking for updates: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Downloads the update ZIP file from GitHub release.
        /// </summary>
        public async Task<string?> DownloadUpdateAsync(GitHubRelease release, IProgress<DownloadProgress>? progress = null)
        {
            if (release == null)
            {
                _logger.WriteLog("Release is null", LogLevel.Error);
                return null;
            }

            try
            {
                // Find ZIP asset
                string versionNumber = release.Version;
                string versionWithV = $"v{versionNumber}";

                GitHubAsset? zipAsset = release.Assets.Find(a =>
                    a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                    (a.Name.Equals($"{versionNumber}.zip", StringComparison.OrdinalIgnoreCase) ||
                     a.Name.Equals($"{versionWithV}.zip", StringComparison.OrdinalIgnoreCase) ||
                     a.Name.Contains(versionNumber, StringComparison.OrdinalIgnoreCase)));

                // Fallback: any ZIP file
                zipAsset ??= release.Assets.Find(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

                if (zipAsset == null)
                {
                    _logger.WriteLog("No ZIP file found in release assets", LogLevel.Error);
                    return null;
                }

                _logger.WriteLog($"Downloading: {zipAsset.Name} ({zipAsset.Size / 1024:N0} KB)", LogLevel.Info);

                // Use browser download URL directly (public repo)
                string downloadUrl = zipAsset.DownloadUrl;
                _logger.WriteLog($"Download URL: {downloadUrl}", LogLevel.Info);

                // Ensure updates directory exists
                string updatesDir = Path.Combine(_rootPath, "updates");
                if (!Directory.Exists(updatesDir))
                {
                    Directory.CreateDirectory(updatesDir);
                }

                string destinationPath = Path.Combine(updatesDir, $"{release.Version}.zip");

                // Check if already downloaded
                if (File.Exists(destinationPath))
                {
                    var existingFileInfo = new FileInfo(destinationPath);
                    if (existingFileInfo.Length == zipAsset.Size)
                    {
                        _logger.WriteLog($"Update already downloaded: {destinationPath}", LogLevel.Info);
                        return destinationPath;
                    }
                    else
                    {
                        _logger.WriteLog("Existing download incomplete - re-downloading", LogLevel.Warning);
                        File.Delete(destinationPath);
                    }
                }

                // Download file
                using (HttpResponseMessage response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.WriteLog($"Download failed: {response.StatusCode}", LogLevel.Error);
                        return null;
                    }

                    long? totalBytes = response.Content.Headers.ContentLength;

                    using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                    using (FileStream fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        byte[] buffer = new byte[8192];
                        long totalBytesRead = 0;
                        int bytesRead;
                        int lastReportedPercent = 0;

                        _logger.WriteLog("Download started", LogLevel.Info);

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;

                            // Report progress at milestones
                            if (progress != null && totalBytes.HasValue)
                            {
                                int currentPercent = (int)((totalBytesRead * 100) / totalBytes.Value);

                                if ((currentPercent >= 25 && lastReportedPercent < 25) ||
                                    (currentPercent >= 50 && lastReportedPercent < 50) ||
                                    (currentPercent >= 75 && lastReportedPercent < 75) ||
                                    (currentPercent >= 100 && lastReportedPercent < 100))
                                {
                                    lastReportedPercent = currentPercent;
                                    progress.Report(new DownloadProgress
                                    {
                                        BytesDownloaded = totalBytesRead,
                                        TotalBytes = totalBytes.Value,
                                        Message = $"Downloaded {totalBytesRead / 1024:N0} KB of {totalBytes.Value / 1024:N0} KB ({currentPercent}%)"
                                    });
                                }
                                else
                                {
                                    progress.Report(new DownloadProgress
                                    {
                                        BytesDownloaded = totalBytesRead,
                                        TotalBytes = totalBytes.Value,
                                        Message = string.Empty
                                    });
                                }
                            }
                        }

                        // Report final 100%
                        if (progress != null && totalBytes.HasValue && lastReportedPercent < 100)
                        {
                            progress.Report(new DownloadProgress
                            {
                                BytesDownloaded = totalBytes.Value,
                                TotalBytes = totalBytes.Value,
                                Message = $"Downloaded {totalBytes.Value / 1024:N0} KB (100%)"
                            });
                        }
                    }
                }

                _logger.WriteLog($"Download complete: {destinationPath}", LogLevel.Success);
                return destinationPath;
            }
            catch (Exception ex)
            {
                _logger.WriteLog($"Error downloading update: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        private GitHubConfig? ReadGitConfig()
        {
            string configPath = Path.Combine(_rootPath, "Config", "git.json");

            if (!File.Exists(configPath))
            {
                return null;
            }

            try
            {
                string jsonContent = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<GitHubConfig>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        public string GetCurrentVersion() => VersionManager.GetCurrentVersion(_rootPath);
    }
}
