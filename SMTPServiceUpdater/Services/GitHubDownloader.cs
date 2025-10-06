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
    /// Handles checking for and downloading updates from GitHub releases.
    /// </summary>
    public class GitHubDownloader
    {
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
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SMTPServiceUpdater/4.2.2");
            }
        }

        /// <summary>
        /// Checks GitHub for the latest release and compares with current version.
        /// </summary>
        /// <returns>GitHubRelease if newer version available, null otherwise</returns>
        public async Task<GitHubRelease?> CheckForUpdateAsync()
        {
            try
            {
                _logger.WriteLog("Checking for updates from GitHub...", LogLevel.Info);

                // Read git.json configuration
                GitHubConfig? config = ReadGitConfig();
                if (config == null)
                {
                    _logger.WriteLog("git.json not found or invalid", LogLevel.Error);
                    return null;
                }

                if (string.IsNullOrWhiteSpace(config.Owner) || string.IsNullOrWhiteSpace(config.Repository))
                {
                    _logger.WriteLog("GitHub Owner or Repository not configured in git.json", LogLevel.Error);
                    return null;
                }

                _logger.WriteLog($"Repository: {config.Owner}/{config.Repository}", LogLevel.Info);

                // Build GitHub API URL
                string apiUrl = $"https://api.github.com/repos/{config.Owner}/{config.Repository}/releases/latest";

                // Create request
                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                
                // Add authorization header if token provided
                if (!string.IsNullOrWhiteSpace(config.Token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.Token);
                }

                // Call GitHub API
                HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.WriteLog($"GitHub API error: {response.StatusCode}", LogLevel.Error);
                    return null;
                }

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

                _logger.WriteLog($"Latest release found: {release.Version}", LogLevel.Success);

                // Compare with current version
                string currentVersion = VersionManager.GetCurrentVersion(_rootPath);
                _logger.WriteLog($"Current version: {currentVersion}", LogLevel.Info);

                if (VersionInfo.TryParse(release.Version, out VersionInfo? latestVersion) &&
                    VersionInfo.TryParse(currentVersion, out VersionInfo? currentVersionInfo))
                {
                    if (latestVersion > currentVersionInfo)
                    {
                        _logger.WriteLog($"New version available: {release.Version}", LogLevel.Success);
                        return release;
                    }
                    else
                    {
                        _logger.WriteLog($"Already running latest version - checked for update anyway", LogLevel.Info);
                        return null;
                    }
                }

                _logger.WriteLog("Could not compare versions", LogLevel.Warning);
                return null;
            }
            catch (HttpRequestException ex)
            {
                _logger.WriteLog($"Network error checking for updates: {ex.Message}", LogLevel.Error);
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
        /// <param name="release">The GitHub release to download</param>
        /// <param name="progress">Progress reporter for download status</param>
        /// <returns>Path to downloaded ZIP file, or null on failure</returns>
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
                GitHubAsset? zipAsset = release.Assets.Find(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
                
                if (zipAsset == null)
                {
                    _logger.WriteLog("No ZIP file found in release assets", LogLevel.Error);
                    return null;
                }

                _logger.WriteLog($"Downloading: {zipAsset.Name} ({zipAsset.Size / 1024:N0} KB)", LogLevel.Info);

                // Ensure updates directory exists
                string updatesDir = Path.Combine(_rootPath, "updates");
                if (!Directory.Exists(updatesDir))
                {
                    Directory.CreateDirectory(updatesDir);
                }

                // Build destination path
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

                // Download file with progress
                using (HttpResponseMessage response = await _httpClient.GetAsync(zipAsset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long? totalBytes = response.Content.Headers.ContentLength;
                    
                    using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                    using (FileStream fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        byte[] buffer = new byte[8192];
                        long totalBytesRead = 0;
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;

                            // Report progress
                            if (progress != null && totalBytes.HasValue)
                            {
                                progress.Report(new DownloadProgress
                                {
                                    BytesDownloaded = totalBytesRead,
                                    TotalBytes = totalBytes.Value,
                                    Message = $"Downloaded {totalBytesRead / 1024:N0} KB of {totalBytes.Value / 1024:N0} KB"
                                });
                            }
                        }
                    }
                }

                _logger.WriteLog($"Download complete: {destinationPath}", LogLevel.Success);
                return destinationPath;
            }
            catch (HttpRequestException ex)
            {
                _logger.WriteLog($"Network error downloading update: {ex.Message}", LogLevel.Error);
                return null;
            }
            catch (IOException ex)
            {
                _logger.WriteLog($"File error downloading update: {ex.Message}", LogLevel.Error);
                return null;
            }
            catch (Exception ex)
            {
                _logger.WriteLog($"Error downloading update: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Reads the git.json configuration file.
        /// </summary>
        /// <returns>GitHubConfig object or null if not found/invalid</returns>
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
                GitHubConfig? config = JsonSerializer.Deserialize<GitHubConfig>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return config;
            }
            catch (JsonException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the current installed version.
        /// </summary>
        /// <returns>Current version string</returns>
        public string GetCurrentVersion()
        {
            return VersionManager.GetCurrentVersion(_rootPath);
        }
    }
}
