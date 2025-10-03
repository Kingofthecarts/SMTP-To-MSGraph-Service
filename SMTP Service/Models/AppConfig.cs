namespace SMTP_Service.Models
{
    public class AppConfig
    {
        public ApplicationSettings ApplicationSettings { get; set; } = new();
        public SmtpSettings SmtpSettings { get; set; } = new();
        public GraphSettings GraphSettings { get; set; } = new();
        public QueueSettings QueueSettings { get; set; } = new();
        public LogSettings LogSettings { get; set; } = new();
    }

    public class ApplicationSettings
    {
        // 0 = Service/Console mode, 1 = Console with Tray (DEFAULT), 2 = Tray only
        public int RunMode { get; set; } = 1;
    }

    public class SmtpSettings
    {
        public int Port { get; set; } = 25;
        public bool RequireAuthentication { get; set; } = true;
        public List<SmtpCredential> Credentials { get; set; } = new();
        public int MaxMessageSizeKb { get; set; } = 51200; // 50MB default
        public bool EnableTls { get; set; } = false;
    }

    public class SmtpCredential
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // Will be encrypted in storage
    }

    public class GraphSettings
    {
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty; // Will be encrypted
        public string SenderEmail { get; set; } = string.Empty;
    }

    public class QueueSettings
    {
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelayMinutes { get; set; } = 5;
        public int MaxQueueSize { get; set; } = 1000;
    }

    public class LogSettings
    {
        public string LogLevel { get; set; } = "Information";
        public string LogLocation { get; set; } = "logs";
    }
}
