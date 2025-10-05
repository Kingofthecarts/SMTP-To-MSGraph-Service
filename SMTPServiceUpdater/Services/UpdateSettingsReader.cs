using System;
using System.IO;
using System.Text.Json;
using SMTPServiceUpdater.Models;

namespace SMTPServiceUpdater.Services
{
    /// <summary>
    /// Reads update settings from smtp-config.json configuration file.
    /// Provides access to AutoDownload and AutoInstall flags for automatic update behavior.
    /// </summary>
    public class UpdateSettingsReader
    {
        /// <summary>
        /// Reads update settings from the smtp-config.json file.
        /// Returns default settings (AutoDownload=false, AutoInstall=false) if file not found or parsing fails.
        /// </summary>
        /// <param name="configPath">Path to the Config folder containing smtp-config.json</param>
        /// <returns>UpdateSettings object with configuration values</returns>
        public static UpdateSettings ReadSettings(string configPath)
        {
            // Default settings - safe defaults (no automatic updates)
            var defaultSettings = new UpdateSettings
            {
                AutoDownload = false,
                AutoInstall = false
            };

            if (string.IsNullOrWhiteSpace(configPath))
            {
                return defaultSettings;
            }

            string smtpConfigFile = Path.Combine(configPath, "smtp-config.json");
            if (!File.Exists(smtpConfigFile))
            {
                return defaultSettings;
            }

            try
            {
                string jsonContent = File.ReadAllText(smtpConfigFile);
                
                // Parse the full config file
                SmtpConfigRoot? config = JsonSerializer.Deserialize<SmtpConfigRoot>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (config?.UpdateSettings != null)
                {
                    return config.UpdateSettings;
                }

                // If UpdateSettings section not found, try AutoUpdateSettings (legacy support)
                if (config?.AutoUpdateSettings != null)
                {
                    return new UpdateSettings
                    {
                        AutoDownload = config.AutoUpdateSettings.AutoDownload,
                        AutoInstall = config.AutoUpdateSettings.AutoInstall
                    };
                }

                return defaultSettings;
            }
            catch (JsonException)
            {
                // Invalid JSON - return defaults
                return defaultSettings;
            }
            catch (IOException)
            {
                // File I/O error - return defaults
                return defaultSettings;
            }
            catch (Exception)
            {
                // Any other error - return defaults (safe fallback)
                return defaultSettings;
            }
        }

        /// <summary>
        /// Reads the full configuration including both UpdateSettings and AutoUpdateSettings.
        /// Used for displaying comprehensive configuration in the UI.
        /// </summary>
        /// <param name="configPath">Path to the Config folder containing smtp-config.json</param>
        /// <returns>SmtpConfigRoot object with all settings, or null if file not found or parsing fails</returns>
        public static SmtpConfigRoot? ReadFullConfig(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
            {
                return null;
            }

            string smtpConfigFile = Path.Combine(configPath, "smtp-config.json");
            if (!File.Exists(smtpConfigFile))
            {
                return null;
            }

            try
            {
                string jsonContent = File.ReadAllText(smtpConfigFile);
                
                SmtpConfigRoot? config = JsonSerializer.Deserialize<SmtpConfigRoot>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return config;
            }
            catch (Exception)
            {
                // Any error - return null (caller handles gracefully)
                return null;
            }
        }
    }
}
