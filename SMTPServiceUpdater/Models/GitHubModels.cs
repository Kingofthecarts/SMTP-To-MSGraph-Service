using System.Text.Json.Serialization;

namespace SMTPServiceUpdater.Models
{
    /// <summary>
    /// Represents a GitHub release with version and download information.
    /// </summary>
    public class GitHubRelease
    {
        /// <summary>
        /// The tag name (version) of the release (e.g., "v4.2.0" or "4.2.0")
        /// </summary>
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        /// <summary>
        /// The name/title of the release
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// List of assets (files) attached to the release
        /// </summary>
        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = new List<GitHubAsset>();

        /// <summary>
        /// When the release was published
        /// </summary>
        [JsonPropertyName("published_at")]
        public DateTime PublishedAt { get; set; }

        /// <summary>
        /// Gets the version string without 'v' prefix
        /// </summary>
        [JsonIgnore]
        public string Version => TagName.TrimStart('v', 'V');
    }

    /// <summary>
    /// Represents a file asset attached to a GitHub release.
    /// </summary>
    public class GitHubAsset
    {
        /// <summary>
        /// Asset ID (required for API downloads)
        /// </summary>
        [JsonPropertyName("id")]
        public long Id { get; set; }

        /// <summary>
        /// The filename of the asset
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// URL to download the asset
        /// </summary>
        [JsonPropertyName("browser_download_url")]
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>
        /// Size of the asset in bytes
        /// </summary>
        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    /// <summary>
    /// Represents download progress for reporting to UI.
    /// </summary>
    public class DownloadProgress
    {
        /// <summary>
        /// Number of bytes downloaded so far
        /// </summary>
        public long BytesDownloaded { get; set; }

        /// <summary>
        /// Total bytes to download
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Percentage complete (0-100)
        /// </summary>
        public int PercentComplete => TotalBytes > 0 
            ? (int)((BytesDownloaded * 100) / TotalBytes) 
            : 0;

        /// <summary>
        /// Human-readable progress message
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Configuration for GitHub repository access.
    /// Root configuration with nested GitHub settings.
    /// </summary>
    public class GitHubConfig
    {
        /// <summary>
        /// Nested GitHub settings object
        /// </summary>
        [JsonPropertyName("GitHub")]
        public GitHubSettings? GitHub { get; set; }

        /// <summary>
        /// Update settings
        /// </summary>
        [JsonPropertyName("UpdateSettings")]
        public UpdateConfigSettings? UpdateSettings { get; set; }
    }

    /// <summary>
    /// GitHub repository settings (nested within GitHubConfig)
    /// </summary>
    public class GitHubSettings
    {
        /// <summary>
        /// GitHub repository owner/organization
        /// </summary>
        [JsonPropertyName("RepoOwner")]
        public string RepoOwner { get; set; } = string.Empty;

        /// <summary>
        /// GitHub repository name
        /// </summary>
        [JsonPropertyName("RepoName")]
        public string RepoName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Update configuration settings
    /// </summary>
    public class UpdateConfigSettings
    {
        [JsonPropertyName("CheckOnStartup")]
        public bool CheckOnStartup { get; set; }

        [JsonPropertyName("ShowPreReleases")]
        public bool ShowPreReleases { get; set; }

        [JsonPropertyName("AutoDownload")]
        public bool AutoDownload { get; set; }

        [JsonPropertyName("LastCheckDate")]
        public DateTime? LastCheckDate { get; set; }
    }
}
