using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SMTP_Service.Models;
using Serilog;

namespace SMTP_Service.Managers
{
    public class ConfigurationManager
    {
        private readonly string _configPath;
        private readonly string _smtpPath;
        private readonly string _userPath;
        private readonly string _graphPath;
        private readonly byte[] _entropy;
        private AppConfig? _config;
        private SmtpConfiguration? _smtpConfig;
        private UserConfiguration? _userConfig;
        private GraphConfiguration? _graphConfig;

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
            _smtpPath = Path.Combine(configFolder, "smtp.json");
            _userPath = Path.Combine(configFolder, "user.json");
            _graphPath = Path.Combine(configFolder, "graph.json");
            
            // Simple entropy for encryption - in production, store this securely
            _entropy = Encoding.UTF8.GetBytes("SMTP-Graph-Relay-2024");
        }

        #region Main Configuration (smtp-config.json)
        public AppConfig LoadConfiguration()
        {
            if (!File.Exists(_configPath))
            {
                Console.WriteLine($"Configuration file not found at: {_configPath}");
                Console.WriteLine("Creating new configuration with defaults...");
                
                _config = CreateDefaultConfiguration();
                SaveConfiguration(_config);
                
                Console.WriteLine("New configuration created successfully.");
                return _config;
            }

            try
            {
                Console.WriteLine($"Loading configuration from: {_configPath}");
                var json = File.ReadAllText(_configPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                
                if (config != null)
                {
                    _config = config;
                    Console.WriteLine("Configuration loaded successfully.");
                    return config;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR loading configuration: {ex.Message}");
            }

            _config = CreateDefaultConfiguration();
            return _config;
        }

        public void SaveConfiguration(AppConfig config)
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var backupPath = _configPath + ".backup";
                    File.Copy(_configPath, backupPath, true);
                    Console.WriteLine($"Configuration backup created at smtp-config.json.backup");
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(_configPath, json);
                
                _config = config;
                Console.WriteLine($"Configuration saved successfully to smtp-config.json");
                Log.Information("Configuration saved to smtp-config.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR saving configuration: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region SMTP Configuration (smtp.json)
        public SmtpConfiguration LoadSmtpConfiguration()
        {
            if (!File.Exists(_smtpPath))
            {
                Console.WriteLine($"SMTP configuration not found at: {_smtpPath}");
                Console.WriteLine("Creating default SMTP configuration...");
                
                _smtpConfig = CreateDefaultSmtpConfiguration();
                SaveSmtpConfiguration(_smtpConfig);
                
                return _smtpConfig;
            }

            try
            {
                var json = File.ReadAllText(_smtpPath);
                var config = JsonSerializer.Deserialize<SmtpConfiguration>(json);
                
                if (config != null)
                {
                    _smtpConfig = config;
                    Console.WriteLine("SMTP configuration loaded successfully.");
                    return config;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR loading SMTP configuration: {ex.Message}");
            }

            _smtpConfig = CreateDefaultSmtpConfiguration();
            return _smtpConfig;
        }

        public void SaveSmtpConfiguration(SmtpConfiguration config)
        {
            try
            {
                if (File.Exists(_smtpPath))
                {
                    var backupPath = _smtpPath + ".backup";
                    File.Copy(_smtpPath, backupPath, true);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(_smtpPath, json);
                
                _smtpConfig = config;
                Console.WriteLine($"SMTP configuration saved to smtp.json");
                Log.Information("SMTP configuration saved to smtp.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR saving SMTP configuration: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region User Configuration (user.json)
        public UserConfiguration LoadUserConfiguration()
        {
            if (!File.Exists(_userPath))
            {
                Console.WriteLine($"User configuration not found at: {_userPath}");
                Console.WriteLine("Creating default user configuration...");
                
                _userConfig = CreateDefaultUserConfiguration();
                SaveUserConfiguration(_userConfig);
                
                return _userConfig;
            }

            try
            {
                var json = File.ReadAllText(_userPath);
                var config = JsonSerializer.Deserialize<UserConfiguration>(json);
                
                if (config != null)
                {
                    // Decrypt passwords
                    DecryptUserData(config);
                    _userConfig = config;
                    Console.WriteLine("User configuration loaded successfully.");
                    return config;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR loading user configuration: {ex.Message}");
            }

            _userConfig = CreateDefaultUserConfiguration();
            return _userConfig;
        }

        public void SaveUserConfiguration(UserConfiguration config)
        {
            try
            {
                if (File.Exists(_userPath))
                {
                    var backupPath = _userPath + ".backup";
                    File.Copy(_userPath, backupPath, true);
                }

                // Create a copy to avoid modifying the original
                var configToSave = CloneUserConfig(config);
                
                // Encrypt passwords
                EncryptUserData(configToSave);

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(configToSave, options);
                File.WriteAllText(_userPath, json);
                
                _userConfig = config;
                Console.WriteLine($"User configuration saved to user.json");
                Log.Information("User configuration saved to user.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR saving user configuration: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region Graph Configuration (graph.json)
        public GraphConfiguration LoadGraphConfiguration()
        {
            if (!File.Exists(_graphPath))
            {
                Console.WriteLine($"Graph configuration not found at: {_graphPath}");
                Console.WriteLine("Creating default Graph configuration...");
                
                _graphConfig = CreateDefaultGraphConfiguration();
                SaveGraphConfiguration(_graphConfig);
                
                return _graphConfig;
            }

            try
            {
                var json = File.ReadAllText(_graphPath);
                var config = JsonSerializer.Deserialize<GraphConfiguration>(json);
                
                if (config != null)
                {
                    // Decrypt sensitive data
                    DecryptGraphData(config);
                    _graphConfig = config;
                    Console.WriteLine("Graph configuration loaded successfully.");
                    return config;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR loading Graph configuration: {ex.Message}");
            }

            _graphConfig = CreateDefaultGraphConfiguration();
            return _graphConfig;
        }

        public void SaveGraphConfiguration(GraphConfiguration config)
        {
            try
            {
                if (File.Exists(_graphPath))
                {
                    var backupPath = _graphPath + ".backup";
                    File.Copy(_graphPath, backupPath, true);
                }

                // Create a copy to avoid modifying the original
                var configToSave = CloneGraphConfig(config);
                
                // Encrypt sensitive data
                EncryptGraphData(configToSave);

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(configToSave, options);
                File.WriteAllText(_graphPath, json);
                
                _graphConfig = config;
                Console.WriteLine($"Graph configuration saved to graph.json");
                Log.Information("Graph configuration saved to graph.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR saving Graph configuration: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region Get Current Configurations
        public AppConfig GetConfiguration() => _config ?? LoadConfiguration();
        public SmtpConfiguration GetSmtpConfiguration() => _smtpConfig ?? LoadSmtpConfiguration();
        public UserConfiguration GetUserConfiguration() => _userConfig ?? LoadUserConfiguration();
        public GraphConfiguration GetGraphConfiguration() => _graphConfig ?? LoadGraphConfiguration();
        #endregion

        #region Default Configurations
        private AppConfig CreateDefaultConfiguration()
        {
            var logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }

            return new AppConfig
            {
                ApplicationSettings = new ApplicationSettings { RunMode = 1 },
                QueueSettings = new QueueSettings
                {
                    MaxRetryAttempts = 3,
                    RetryDelayMinutes = 5,
                    MaxQueueSize = 1000
                },
                LogSettings = new LogSettings
                {
                    LogLevel = "Information",
                    LogLocation = "logs"
                },
                UpdateSettings = new UpdateSettings()
            };
        }

        private SmtpConfiguration CreateDefaultSmtpConfiguration()
        {
            return new SmtpConfiguration
            {
                Port = 25,
                BindAddress = "0.0.0.0",
                RequireAuthentication = true,
                MaxMessageSizeKb = 51200,
                EnableTls = false,
                SmtpFlowEnabled = true,
                SendDelayMs = 1000
            };
        }

        private UserConfiguration CreateDefaultUserConfiguration()
        {
            return new UserConfiguration
            {
                Credentials = new List<SmtpCredential>
                {
                    new SmtpCredential 
                    { 
                        Username = "user1", 
                        Password = "changeme" 
                    }
                }
            };
        }

        private GraphConfiguration CreateDefaultGraphConfiguration()
        {
            return new GraphConfiguration
            {
                TenantId = "your-tenant-id",
                ClientId = "your-client-id",
                ClientSecret = "your-client-secret",
                SenderEmail = "noreply@yourdomain.com"
            };
        }
        #endregion

        #region Encryption/Decryption
        private void EncryptUserData(UserConfiguration config)
        {
            foreach (var cred in config.Credentials)
            {
                if (!string.IsNullOrEmpty(cred.Password) && !cred.Password.StartsWith("ENC:"))
                {
                    cred.Password = "ENC:" + EncryptString(cred.Password);
                }
            }
        }

        private void DecryptUserData(UserConfiguration config)
        {
            foreach (var cred in config.Credentials)
            {
                if (cred.Password.StartsWith("ENC:"))
                {
                    cred.Password = DecryptString(cred.Password.Substring(4));
                }
            }
        }

        private void EncryptGraphData(GraphConfiguration config)
        {
            if (!string.IsNullOrEmpty(config.ClientSecret) && !config.ClientSecret.StartsWith("ENC:"))
            {
                config.ClientSecret = "ENC:" + EncryptString(config.ClientSecret);
            }

            if (!string.IsNullOrEmpty(config.TenantId) && !config.TenantId.StartsWith("ENC:"))
            {
                config.TenantId = "ENC:" + EncryptString(config.TenantId);
            }

            if (!string.IsNullOrEmpty(config.ClientId) && !config.ClientId.StartsWith("ENC:"))
            {
                config.ClientId = "ENC:" + EncryptString(config.ClientId);
            }
        }

        private void DecryptGraphData(GraphConfiguration config)
        {
            if (config.ClientSecret.StartsWith("ENC:"))
            {
                config.ClientSecret = DecryptString(config.ClientSecret.Substring(4));
            }

            if (config.TenantId.StartsWith("ENC:"))
            {
                config.TenantId = DecryptString(config.TenantId.Substring(4));
            }

            if (config.ClientId.StartsWith("ENC:"))
            {
                config.ClientId = DecryptString(config.ClientId.Substring(4));
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
                return plainText;
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
                return encryptedText;
            }
        }
        #endregion

        #region Flow Control
        public void SetSmtpFlowEnabled(bool enabled)
        {
            var smtpConfig = GetSmtpConfiguration();
            smtpConfig.SmtpFlowEnabled = enabled;
            SaveSmtpConfiguration(smtpConfig);
        }
        #endregion

        #region Cloning
        private UserConfiguration CloneUserConfig(UserConfiguration config)
        {
            var json = JsonSerializer.Serialize(config);
            return JsonSerializer.Deserialize<UserConfiguration>(json) ?? new UserConfiguration();
        }

        private GraphConfiguration CloneGraphConfig(GraphConfiguration config)
        {
            var json = JsonSerializer.Serialize(config);
            return JsonSerializer.Deserialize<GraphConfiguration>(json) ?? new GraphConfiguration();
        }
        #endregion
    }
}
