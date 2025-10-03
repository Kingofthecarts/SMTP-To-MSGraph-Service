using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Serilog;

namespace SMTP_Service.Services
{
    public class GitConfigurationManager
    {
        // Must match PowerShell script
        private const string ENCRYPTION_KEY = "SMTP2GraphRelay2025SecureKey!32c";
        private const string SALT = "SMTPRelay2025";
        private const int ITERATIONS = 1000;
        
        private readonly string _configPath;
        private GitConfiguration? _config;
        private string? _decryptedOwner;
        private string? _decryptedRepo;
        private string? _decryptedToken;
        
        public GitConfigurationManager()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            _configPath = Path.Combine(appDir, "config", "git.json");
            LoadConfiguration();
        }
        
        public string GetGitHubToken()
        {
            if (_decryptedToken != null)
                return _decryptedToken;
                
            if (string.IsNullOrEmpty(_config?.GitHub?.Token))
            {
                Log.Warning("GitHub token not configured");
                return string.Empty;
            }
            
            _decryptedToken = DecryptString(_config.GitHub.Token);
            if (string.IsNullOrEmpty(_decryptedToken))
            {
                Log.Error("Failed to decrypt GitHub token");
                return string.Empty;
            }
            
            return _decryptedToken;
        }
        
        private string DecryptString(string encryptedText)
        {
            try
            {
                var saltBytes = Encoding.UTF8.GetBytes(SALT);
                
                using var passwordDerive = new Rfc2898DeriveBytes(ENCRYPTION_KEY, saltBytes, ITERATIONS, HashAlgorithmName.SHA256);
                var keyBytes = passwordDerive.GetBytes(32); // 256-bit
                var ivBytes = passwordDerive.GetBytes(16);  // 128-bit
                
                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                
                var encryptedBytes = Convert.FromBase64String(encryptedText);
                
                using var decryptor = aes.CreateDecryptor();
                using var memoryStream = new MemoryStream(encryptedBytes);
                using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                using var reader = new StreamReader(cryptoStream);
                
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to decrypt token");
                return string.Empty;
            }
        }
        
        private void LoadConfiguration()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    Log.Warning($"Git configuration not found at {_configPath}");
                    _config = null;
                    return;
                }
                
                var json = File.ReadAllText(_configPath);
                _config = JsonSerializer.Deserialize<GitConfiguration>(json);
                
                // Decrypt and cache the values
                if (_config?.GitHub != null)
                {
                    if (!string.IsNullOrEmpty(_config.GitHub.RepoOwner))
                        _decryptedOwner = DecryptString(_config.GitHub.RepoOwner);
                    
                    if (!string.IsNullOrEmpty(_config.GitHub.RepoName))
                        _decryptedRepo = DecryptString(_config.GitHub.RepoName);
                    
                    // Token is decrypted on demand via GetGitHubToken()
                }
                
                Log.Information($"Loaded git configuration (encrypted)");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load git configuration");
                _config = null;
            }
        }
        
        // Public properties - now return decrypted values
        public string RepoOwner 
        {
            get
            {
                if (_decryptedOwner != null)
                    return _decryptedOwner;
                    
                if (string.IsNullOrEmpty(_config?.GitHub?.RepoOwner))
                    return "";
                    
                _decryptedOwner = DecryptString(_config.GitHub.RepoOwner);
                return _decryptedOwner ?? "";
            }
        }
        
        public string RepoName 
        {
            get
            {
                if (_decryptedRepo != null)
                    return _decryptedRepo;
                    
                if (string.IsNullOrEmpty(_config?.GitHub?.RepoName))
                    return "";
                    
                _decryptedRepo = DecryptString(_config.GitHub.RepoName);
                return _decryptedRepo ?? "";
            }
        }
        
        public bool CheckOnStartup => _config?.UpdateSettings?.CheckOnStartup ?? false;
        public bool IsConfigured => _config != null && !string.IsNullOrEmpty(_config.GitHub?.Token);
        
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
            public string? Token { get; set; }
            public string? UpdateCheckUrl { get; set; }
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
