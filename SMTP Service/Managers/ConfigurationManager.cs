using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SMTP_Service.Models;

namespace SMTP_Service.Managers
{
    public class ConfigurationManager
    {
        private readonly string _configPath;
        private readonly byte[] _entropy;
        private AppConfig? _config;

        public ConfigurationManager(string? configPath = null)
        {
            // Use the executable directory, not the current directory
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            
            // Create config folder if it doesn't exist
            var configFolder = Path.Combine(baseDirectory, "config");
            if (!Directory.Exists(configFolder))
            {
                Directory.CreateDirectory(configFolder);
            }
            
            _configPath = configPath ?? Path.Combine(configFolder, "smtp-config.json");
            // Simple entropy for encryption - in production, store this securely
            _entropy = Encoding.UTF8.GetBytes("SMTP-Graph-Relay-2024");
        }

        public AppConfig LoadConfiguration()
        {
            if (!File.Exists(_configPath))
            {
                _config = CreateDefaultConfiguration();
                SaveConfiguration(_config);
                return _config;
            }

            try
            {
                var json = File.ReadAllText(_configPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                
                if (config != null)
                {
                    // Decrypt sensitive fields
                    DecryptSensitiveData(config);
                    _config = config;
                    return config;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
            }

            _config = CreateDefaultConfiguration();
            return _config;
        }

        public void SaveConfiguration(AppConfig config)
        {
            try
            {
                // Create a copy to avoid modifying the original
                var configToSave = CloneConfig(config);
                
                // Encrypt sensitive fields
                EncryptSensitiveData(configToSave);

                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                };
                
                var json = JsonSerializer.Serialize(configToSave, options);
                File.WriteAllText(_configPath, json);
                
                _config = config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
                throw;
            }
        }

        public AppConfig GetConfiguration()
        {
            return _config ?? LoadConfiguration();
        }

        private AppConfig CreateDefaultConfiguration()
        {
            // Ensure logs directory exists
            var logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }

            return new AppConfig
            {
                ApplicationSettings = new ApplicationSettings
                {
                    RunMode = 0 // Default: Service/Console mode
                },
                SmtpSettings = new SmtpSettings
                {
                    Port = 25,
                    RequireAuthentication = true,
                    Credentials = new List<SmtpCredential>
                    {
                        new SmtpCredential 
                        { 
                            Username = "user1", 
                            Password = "changeme" 
                        }
                    },
                    MaxMessageSizeKb = 10240,
                    EnableTls = false
                },
                GraphSettings = new GraphSettings
                {
                    TenantId = "your-tenant-id",
                    ClientId = "your-client-id",
                    ClientSecret = "your-client-secret",
                    SenderEmail = "noreply@yourdomain.com"
                },
                QueueSettings = new QueueSettings
                {
                    MaxRetryAttempts = 3,
                    RetryDelayMinutes = 5,
                    MaxQueueSize = 1000
                },
                LogSettings = new LogSettings
                {
                    LogLevel = "Information",
                    LogFilePath = Path.Combine(logsDir, "smtp-relay.log"),
                    RollingInterval = "Day"
                }
            };
        }

        private void EncryptSensitiveData(AppConfig config)
        {
            // Encrypt SMTP passwords
            foreach (var cred in config.SmtpSettings.Credentials)
            {
                if (!string.IsNullOrEmpty(cred.Password) && !cred.Password.StartsWith("ENC:"))
                {
                    cred.Password = "ENC:" + EncryptString(cred.Password);
                }
            }

            // Encrypt Graph credentials
            if (!string.IsNullOrEmpty(config.GraphSettings.ClientSecret) && 
                !config.GraphSettings.ClientSecret.StartsWith("ENC:"))
            {
                config.GraphSettings.ClientSecret = "ENC:" + EncryptString(config.GraphSettings.ClientSecret);
            }

            if (!string.IsNullOrEmpty(config.GraphSettings.TenantId) && 
                !config.GraphSettings.TenantId.StartsWith("ENC:"))
            {
                config.GraphSettings.TenantId = "ENC:" + EncryptString(config.GraphSettings.TenantId);
            }

            if (!string.IsNullOrEmpty(config.GraphSettings.ClientId) && 
                !config.GraphSettings.ClientId.StartsWith("ENC:"))
            {
                config.GraphSettings.ClientId = "ENC:" + EncryptString(config.GraphSettings.ClientId);
            }
        }

        private void DecryptSensitiveData(AppConfig config)
        {
            // Decrypt SMTP passwords
            foreach (var cred in config.SmtpSettings.Credentials)
            {
                if (cred.Password.StartsWith("ENC:"))
                {
                    cred.Password = DecryptString(cred.Password.Substring(4));
                }
            }

            // Decrypt Graph credentials
            if (config.GraphSettings.ClientSecret.StartsWith("ENC:"))
            {
                config.GraphSettings.ClientSecret = DecryptString(config.GraphSettings.ClientSecret.Substring(4));
            }

            if (config.GraphSettings.TenantId.StartsWith("ENC:"))
            {
                config.GraphSettings.TenantId = DecryptString(config.GraphSettings.TenantId.Substring(4));
            }

            if (config.GraphSettings.ClientId.StartsWith("ENC:"))
            {
                config.GraphSettings.ClientId = DecryptString(config.GraphSettings.ClientId.Substring(4));
            }
        }

        private string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                var data = Encoding.UTF8.GetBytes(plainText);
                var encrypted = ProtectedData.Protect(data, _entropy, DataProtectionScope.LocalMachine);
                return Convert.ToBase64String(encrypted);
            }
            catch
            {
                return plainText; // Return as-is if encryption fails
            }
        }

        private string DecryptString(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            try
            {
                var data = Convert.FromBase64String(encryptedText);
                var decrypted = ProtectedData.Unprotect(data, _entropy, DataProtectionScope.LocalMachine);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                return encryptedText; // Return as-is if decryption fails
            }
        }

        private AppConfig CloneConfig(AppConfig config)
        {
            var json = JsonSerializer.Serialize(config);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
    }
}
