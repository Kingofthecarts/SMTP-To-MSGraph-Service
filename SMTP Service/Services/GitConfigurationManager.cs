using System;
using System.IO;
using System.Text.Json;
using Serilog;

namespace SMTP_Service.Services
{
    /// <summary>
    /// Manages GitHub configuration for update checks.
    /// Reads plaintext values from git.json for public repository access.
    /// Falls back to default public repository if config is missing or invalid.
    /// </summary>
    public class GitConfigurationManager
    {
        // Default public repository - used as fallback if git.json is missing or invalid
        private const string DefaultRepoOwner = "Kingofthecarts";
        private const string DefaultRepoName = "SMTP-To-MSGraph-Service";

        private readonly string _configPath;
        private GitConfiguration? _config;
        private bool _usingDefaults;

        public GitConfigurationManager()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            _configPath = Path.Combine(appDir, "config", "git.json");
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    Log.Warning($"Git configuration not found at {_configPath}, using defaults");
                    _config = null;
                    _usingDefaults = true;
                    return;
                }

                var json = File.ReadAllText(_configPath);
                _config = JsonSerializer.Deserialize<GitConfiguration>(json);

                // Check if config values are valid (not encrypted/base64)
                var owner = _config?.GitHub?.RepoOwner ?? "";
                var repo = _config?.GitHub?.RepoName ?? "";

                if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo) ||
                    owner.Contains('=') || owner.Contains('/') ||
                    repo.Contains('=') || repo.Contains('/'))
                {
                    Log.Warning("Git configuration appears invalid or encrypted, using defaults");
                    _usingDefaults = true;
                }
                else
                {
                    _usingDefaults = false;
                    Log.Information("Loaded git configuration");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load git configuration, using defaults");
                _config = null;
                _usingDefaults = true;
            }
        }

        public string RepoOwner => _usingDefaults ? DefaultRepoOwner : (_config?.GitHub?.RepoOwner ?? DefaultRepoOwner);
        public string RepoName => _usingDefaults ? DefaultRepoName : (_config?.GitHub?.RepoName ?? DefaultRepoName);
        public bool CheckOnStartup => _config?.UpdateSettings?.CheckOnStartup ?? false;
        public bool IsConfigured => true; // Always configured with defaults

        // Configuration classes
        public class GitConfiguration
        {
            public GitHubSettings? GitHub { get; set; }
            public UpdateSettings? UpdateSettings { get; set; }
        }

        public class GitHubSettings
        {
            public string? RepoOwner { get; set; }
            public string? RepoName { get; set; }
        }

        public class UpdateSettings
        {
            public bool CheckOnStartup { get; set; }
            public bool ShowPreReleases { get; set; }
            public bool AutoDownload { get; set; }
            public DateTime? LastCheckDate { get; set; }
        }
    }
}
