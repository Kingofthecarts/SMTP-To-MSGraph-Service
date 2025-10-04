namespace SMTP_Service.Models
{
    public class AppConfig
    {
        public ApplicationSettings ApplicationSettings { get; set; } = new();
        public QueueSettings QueueSettings { get; set; } = new();
        public LogSettings LogSettings { get; set; } = new();
        public UpdateSettings UpdateSettings { get; set; } = new();
        public SmtpConfiguration SmtpSettings { get; set; } = new();
        public GraphConfiguration GraphSettings { get; set; } = new();
    }

    public class ApplicationSettings
    {
        // 0 = Service/Console mode, 1 = Console with Tray (DEFAULT), 2 = Tray only
        public int RunMode { get; set; } = 1;
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

    public class UpdateSettings
    {
        // Schedule Settings
        public bool AutoUpdateEnabled { get; set; } = false;
        public UpdateCheckFrequency CheckFrequency { get; set; } = UpdateCheckFrequency.Daily;
        public TimeSpan CheckTime { get; set; } = new TimeSpan(2, 0, 0); // 2 AM default
        public DayOfWeek WeeklyCheckDay { get; set; } = DayOfWeek.Sunday; // For weekly checks
        
        // Behavior Settings
        public bool AutoDownload { get; set; } = false;
        public bool AutoInstall { get; set; } = false; // Only enabled if AutoDownload is true
        public bool CheckOnStartup { get; set; } = true;
        
        // Tracking
        public DateTime? LastCheckDate { get; set; }
        public DateTime? LastUpdateDate { get; set; }
        public string? LastInstalledVersion { get; set; }
    }

    public enum UpdateCheckFrequency
    {
        Daily = 1,
        Weekly = 2
    }
}
