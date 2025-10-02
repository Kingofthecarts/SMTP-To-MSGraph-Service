namespace SMTP_Service.Models
{
    public class Statistics
    {
        public GlobalStats Global { get; set; } = new();
        public Dictionary<string, UserStats> UserStats { get; set; } = new();
    }

    public class GlobalStats
    {
        public long TotalSuccess { get; set; } = 0;
        public long TotalFailed { get; set; } = 0;
        public DateTime? LastSuccessDate { get; set; }
        public DateTime? LastFailureDate { get; set; }
    }

    public class UserStats
    {
        public string Username { get; set; } = string.Empty;
        public long TotalSuccess { get; set; } = 0;
        public long TotalFailed { get; set; } = 0;
        public DateTime? LastSuccessDate { get; set; }
        public DateTime? LastFailureDate { get; set; }
    }
}
