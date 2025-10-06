using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using SMTPServiceUpdater.Models;
using SMTPServiceUpdater.Helpers;

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
        private GitHubConfig? _cachedConfig;

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
        /// <returns>GitHubRelease if newer version available, null otherwise</returns>
        public async Task<GitHubRelease?> CheckForUpdateAsync()
        {
            try
            {
                _logger.WriteLog("Connecting to GitHub...", LogLevel.Info);
                _logger.WriteLog("Checking for updates from GitHub...", LogLevel.Info);

                // Read git.json configuration
                GitHubConfig? config = ReadGitConfig();
                _cachedConfig = config; // Cache for download method
                
                if (config?.GitHub == null)
                {
                    _logger.WriteLog("git.json not found or invalid", LogLevel.Error);
                    return null;
                }

                // Decrypt the repository owner and name
                string owner = GitConfigDecryptor.DecryptString(config.GitHub.RepoOwner);
                string repository = GitConfigDecryptor.DecryptString(config.GitHub.RepoName);

                if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repository))
                {
                    _logger.WriteLog("GitHub Owner or Repository not configured or decryption failed", LogLevel.Error);
                    _logger.WriteLog("Check git.json configuration file", LogLevel.Error);
                    return null;
                }

                _logger.WriteLog($"Repository: {owner}/{repository}", LogLevel.Info);

                // Get and display current version first
                string currentVersion = VersionManager.GetCurrentVersion(_rootPath);
                _logger.WriteLog($"Current installed version: {currentVersion}", LogLevel.Info);

                // Build GitHub API URL
                string apiUrl = $"https://api.github.com/repos/{owner}/{repository}/releases/latest";
                _logger.WriteLog($"Querying GitHub API...", LogLevel.Info);

                // Create request
                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                
                // Add authorization header if token provided - use "token" format for GitHub
                if (!string.IsNullOrWhiteSpace(config.GitHub.Token))
                {
                    string decryptedToken = GitConfigDecryptor.DecryptString(config.GitHub.Token);
                    if (!string.IsNullOrWhiteSpace(decryptedToken))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("token", decryptedToken);
                    }
                }

                // Call GitHub API
                HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.WriteLog($"GitHub API error: {response.StatusCode}", LogLevel.Error);
                    _logger.WriteLog($"Unable to check for updates", LogLevel.Error);
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
                else
                {
                    _logger.WriteLog("WARNING: No assets found in this release!", LogLevel.Warning);
                }

                // Compare versions

                if (VersionInfo.TryParse(release.Version, out VersionInfo? latestVersion) &&
                    VersionInfo.TryParse(currentVersion, out VersionInfo? currentVersionInfo))
                {
                    // Show version comparison
                    _logger.WriteLog($"Version comparison: {currentVersion} vs {release.Version}", LogLevel.Info);
                    
                    if (latestVersion > currentVersionInfo)
                    {
                        _logger.WriteLog($"UPDATE AVAILABLE: New version {release.Version} is available (current: {currentVersion})", LogLevel.Success);
                        return release;
                    }
                    else if (latestVersion == currentVersionInfo)
                    {
                        _logger.WriteLog($"You are running the latest version ({currentVersion})", LogLevel.Success);
                        _logger.WriteLog($"No update needed", LogLevel.Info);
                        return null;
                    }
                    else
                    {
                        _logger.WriteLog($"Current version ({currentVersion}) is newer than GitHub ({release.Version})", LogLevel.Warning);
                        _logger.WriteLog($"You may be running a development version", LogLevel.Info);
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
                // Find ZIP asset - look for version pattern in filename
                GitHubAsset? zipAsset = null;
                
                // Try multiple naming patterns:
                // 1. Exact version match: "4.2.3.zip"
                // 2. With v prefix: "v4.2.3.zip"
                // 3. Contains version: "SMTP-Service-4.2.3.zip"
                string versionNumber = release.Version; // e.g., "4.2.3"
                string versionWithV = $"v{versionNumber}"; // e.g., "v4.2.3"
                
                zipAsset = release.Assets.Find(a => 
                    a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                    (a.Name.Equals($"{versionNumber}.zip", StringComparison.OrdinalIgnoreCase) ||
                     a.Name.Equals($"{versionWithV}.zip", StringComparison.OrdinalIgnoreCase) ||
                     a.Name.Contains(versionNumber, StringComparison.OrdinalIgnoreCase)));
                
                // If still not found, just get any ZIP file
                if (zipAsset == null)
                {
                    zipAsset = release.Assets.Find(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
                }
                
                if (zipAsset == null)
                {
                    _logger.WriteLog("No ZIP file found in release assets", LogLevel.Error);
                    _logger.WriteLog($"Looking for version: {release.Version}", LogLevel.Error);
                    
                    if (release.Assets.Count > 0)
                    {
                        _logger.WriteLog($"Available assets ({release.Assets.Count}):", LogLevel.Info);
                        foreach (var asset in release.Assets)
                        {
                            _logger.WriteLog($"  - {asset.Name}", LogLevel.Info);
                        }
                    }
                    else
                    {
                        _logger.WriteLog("No assets found in this release", LogLevel.Warning);
                    }
                    
                    return null;
                }

                _logger.WriteLog($"Downloading: {zipAsset.Name} ({zipAsset.Size / 1024:N0} KB)", LogLevel.Info);
                _logger.WriteLog($"Asset ID: {zipAsset.Id}", LogLevel.Info);

                // Build API URL for authenticated download (required for private repos)
                // GitHub requires using the API URL with asset ID, not the browser download URL
                string owner = GitConfigDecryptor.DecryptString(_cachedConfig!.GitHub!.RepoOwner);
                string repo = GitConfigDecryptor.DecryptString(_cachedConfig.GitHub.RepoName);
                string apiDownloadUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/assets/{zipAsset.Id}";
                
                _logger.WriteLog($"Download URL: {apiDownloadUrl}", LogLevel.Info);

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
                var downloadRequest = new HttpRequestMessage(HttpMethod.Get, apiDownloadUrl);
                
                // Add required Accept header for GitHub release assets
                downloadRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
                
                // Add authentication - use "token" format for GitHub (not "Bearer")
                if (!string.IsNullOrWhiteSpace(_cachedConfig?.GitHub?.Token))
                {
                    string decryptedToken = GitConfigDecryptor.DecryptString(_cachedConfig.GitHub.Token);
                    if (!string.IsNullOrWhiteSpace(decryptedToken))
                    {
                        downloadRequest.Headers.Authorization = new AuthenticationHeaderValue("token", decryptedToken);
                        _logger.WriteLog("Using authenticated download (private repository)", LogLevel.Info);
                    }
                }
                
                using (HttpResponseMessage response = await _httpClient.SendAsync(downloadRequest, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.WriteLog($"Download failed: {response.StatusCode} - {response.ReasonPhrase}", LogLevel.Error);
                        _logger.WriteLog($"URL attempted: {apiDownloadUrl}", LogLevel.Error);
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

                            // Report progress at 25%, 50%, 75%, and 100%
                            if (progress != null && totalBytes.HasValue)
                            {
                                int currentPercent = (int)((totalBytesRead * 100) / totalBytes.Value);
                                
                                // Check for milestone percentages
                                if ((currentPercent >= 25 && lastReportedPercent < 25) ||
                                    (currentPercent >= 50 && lastReportedPercent < 50) ||
                                    (currentPercent >= 75 && lastReportedPercent < 75) ||
                                    (currentPercent >= 100 && lastReportedPercent < 100))
                                {
                                    lastReportedPercent = currentPercent;
                                    
                                    var progressReport = new DownloadProgress
                                    {
                                        BytesDownloaded = totalBytesRead,
                                        TotalBytes = totalBytes.Value,
                                        Message = $"Downloaded {totalBytesRead / 1024:N0} KB of {totalBytes.Value / 1024:N0} KB ({currentPercent}%)"
                                    };
                                    
                                    progress.Report(progressReport);
                                }
                                else
                                {
                                    // Still update the progress bar continuously, just don't include a message
                                    progress.Report(new DownloadProgress
                                    {
                                        BytesDownloaded = totalBytesRead,
                                        TotalBytes = totalBytes.Value,
                                        Message = string.Empty // No message for non-milestone updates
                                    });
                                }
                            }
                        }
                        
                        // Report final 100% if not already reported
                        if (progress != null && totalBytes.HasValue && lastReportedPercent < 100)
                        {
                            progress.Report(new DownloadProgress
                            {
                                BytesDownloaded = totalBytes.Value,
                                TotalBytes = totalBytes.Value,
                                Message = $"Downloaded {totalBytes.Value / 1024:N0} KB of {totalBytes.Value / 1024:N0} KB (100%)"
                            });
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
