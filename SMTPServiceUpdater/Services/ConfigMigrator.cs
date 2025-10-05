using System;
using System.IO;
using System.Text.Json;
using SMTPServiceUpdater.Models;

namespace SMTPServiceUpdater.Services
{
    /// <summary>
    /// Handles configuration migration from monolithic smtp-config.json to split configuration files.
    /// Migrates SmtpSettings to smtp.json, credentials to user.json, and GraphSettings to graph.json.
    /// </summary>
    public class ConfigMigrator
    {
        private readonly UpdateLogger _logger;

        public ConfigMigrator(UpdateLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Migrates configuration from smtp-config.json to split files if needed.
        /// Creates pre-migration backup before making changes.
        /// </summary>
        /// <param name="configPath">Path to the Config folder</param>
        /// <returns>True if migration succeeded or wasn't needed, false on error</returns>
        public bool MigrateConfiguration(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
            {
                _logger.WriteLog("Config path is null or empty", LogLevel.Error);
                return false;
            }

            if (!Directory.Exists(configPath))
            {
                _logger.WriteLog($"Config directory does not exist: {configPath}", LogLevel.Error);
                return false;
            }

            string smtpConfigFile = Path.Combine(configPath, "smtp-config.json");
            if (!File.Exists(smtpConfigFile))
            {
                _logger.WriteLog("smtp-config.json not found - creating default split configs", LogLevel.Warning);
                CreateDefaultSplitConfigs(configPath);
                return true;
            }

            try
            {
                string jsonContent = File.ReadAllText(smtpConfigFile);
                using JsonDocument doc = JsonDocument.Parse(jsonContent);
                JsonElement root = doc.RootElement;

                bool hasSmtpSettings = root.TryGetProperty("SmtpSettings", out _);
                bool hasGraphSettings = root.TryGetProperty("GraphSettings", out _);

                if (!hasSmtpSettings && !hasGraphSettings)
                {
                    _logger.WriteLog("No migration needed - smtp-config.json already migrated", LogLevel.Info);
                    return true;
                }

                _logger.WriteLog("Configuration migration needed - creating backup", LogLevel.Info);
                
                // Create pre-migration backup
                string backupPath = smtpConfigFile + ".pre-migration.backup";
                File.Copy(smtpConfigFile, backupPath, overwrite: true);
                _logger.WriteLog($"Pre-migration backup created: {backupPath}", LogLevel.Success);

                // Extract and save SmtpSettings
                if (hasSmtpSettings)
                {
                    JsonElement smtpSettings = root.GetProperty("SmtpSettings");
                    string smtpJson = JsonSerializer.Serialize(smtpSettings, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(Path.Combine(configPath, "smtp.json"), smtpJson);
                    _logger.WriteLog("Migrated SmtpSettings to smtp.json", LogLevel.Success);

                    // Extract credentials if present
                    if (smtpSettings.TryGetProperty("Credentials", out JsonElement credentials))
                    {
                        var userConfig = new { Credentials = credentials };
                        string userJson = JsonSerializer.Serialize(userConfig, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(Path.Combine(configPath, "user.json"), userJson);
                        _logger.WriteLog("Migrated Credentials to user.json", LogLevel.Success);
                    }
                }

                // Extract and save GraphSettings
                if (hasGraphSettings)
                {
                    JsonElement graphSettings = root.GetProperty("GraphSettings");
                    string graphJson = JsonSerializer.Serialize(graphSettings, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(Path.Combine(configPath, "graph.json"), graphJson);
                    _logger.WriteLog("Migrated GraphSettings to graph.json", LogLevel.Success);
                }

                // Remove migrated sections from smtp-config.json
                var updatedConfig = new
                {
                    QueueSettings = root.TryGetProperty("QueueSettings", out var qs) ? qs : (JsonElement?)null,
                    ApplicationSettings = root.TryGetProperty("ApplicationSettings", out var apps) ? apps : (JsonElement?)null,
                    LogSettings = root.TryGetProperty("LogSettings", out var logs) ? logs : (JsonElement?)null,
                    UpdateSettings = root.TryGetProperty("UpdateSettings", out var updates) ? updates : (JsonElement?)null,
                    AutoUpdateSettings = root.TryGetProperty("AutoUpdateSettings", out var autoUpdates) ? autoUpdates : (JsonElement?)null
                };

                string updatedJson = JsonSerializer.Serialize(updatedConfig, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(smtpConfigFile, updatedJson);
                _logger.WriteLog("Updated smtp-config.json - removed migrated sections", LogLevel.Success);

                _logger.WriteLog("Configuration migration completed successfully", LogLevel.Success);
                return true;
            }
            catch (Exception ex)
            {
                _logger.WriteLog($"Configuration migration failed: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Creates default split configuration files if they don't exist.
        /// </summary>
        /// <param name="configPath">Path to the Config folder</param>
        public void CreateDefaultSplitConfigs(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
            {
                _logger.WriteLog("Config path is null or empty", LogLevel.Error);
                return;
            }

            try
            {
                // Create smtp.json with defaults
                string smtpFile = Path.Combine(configPath, "smtp.json");
                if (!File.Exists(smtpFile))
                {
                    var defaultSmtp = new
                    {
                        Port = 25,
                        BindAddress = "0.0.0.0",
                        RequireAuthentication = true,
                        EnableStartTls = true,
                        MaxMessageSize = 10485760,
                        MaxRecipients = 100,
                        ConnectionTimeout = 30,
                        MaxConcurrentConnections = 10
                    };
                    string smtpJson = JsonSerializer.Serialize(defaultSmtp, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(smtpFile, smtpJson);
                    _logger.WriteLog("Created default smtp.json", LogLevel.Success);
                }

                // Create user.json with defaults
                string userFile = Path.Combine(configPath, "user.json");
                if (!File.Exists(userFile))
                {
                    var defaultUser = new
                    {
                        Credentials = new object[] { }
                    };
                    string userJson = JsonSerializer.Serialize(defaultUser, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(userFile, userJson);
                    _logger.WriteLog("Created default user.json", LogLevel.Success);
                }

                // Create graph.json with defaults
                string graphFile = Path.Combine(configPath, "graph.json");
                if (!File.Exists(graphFile))
                {
                    var defaultGraph = new
                    {
                        TenantId = "",
                        ClientId = "",
                        ClientSecret = "",
                        SenderEmail = ""
                    };
                    string graphJson = JsonSerializer.Serialize(defaultGraph, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(graphFile, graphJson);
                    _logger.WriteLog("Created default graph.json", LogLevel.Success);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog($"Failed to create default configs: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Creates default smtp-config.json if it doesn't exist.
        /// </summary>
        /// <param name="configPath">Path to the Config folder</param>
        public void CreateDefaultSmtpConfig(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
            {
                _logger.WriteLog("Config path is null or empty", LogLevel.Error);
                return;
            }

            string smtpConfigFile = Path.Combine(configPath, "smtp-config.json");
            if (File.Exists(smtpConfigFile))
            {
                return;
            }

            try
            {
                var defaultConfig = new
                {
                    QueueSettings = new
                    {
                        ProcessingInterval = 60,
                        MaxRetries = 3,
                        RetryDelay = 300
                    },
                    ApplicationSettings = new
                    {
                        ServiceName = "SMTP Service",
                        ServiceDisplayName = "SMTP Service",
                        ServiceDescription = "SMTP relay service with queue management"
                    },
                    LogSettings = new
                    {
                        LogLevel = "Information",
                        RetentionDays = 30
                    },
                    UpdateSettings = new
                    {
                        AutoDownload = false,
                        AutoInstall = false
                    },
                    AutoUpdateSettings = new
                    {
                        AutoUpdateEnabled = false,
                        CheckFrequency = "Weekly",
                        CheckTime = "03:00",
                        WeeklyCheckDay = "Sunday",
                        AutoDownload = false,
                        AutoInstall = false,
                        CheckOnStartup = true,
                        LastCheckDate = (string?)null,
                        LastUpdateDate = (string?)null,
                        LastInstalledVersion = (string?)null
                    }
                };

                string configJson = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(smtpConfigFile, configJson);
                _logger.WriteLog("Created default smtp-config.json", LogLevel.Success);
            }
            catch (Exception ex)
            {
                _logger.WriteLog($"Failed to create default smtp-config.json: {ex.Message}", LogLevel.Error);
            }
        }
    }
}
