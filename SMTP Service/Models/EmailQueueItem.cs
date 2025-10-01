namespace SMTP_Service.Models
{
    public class EmailQueueItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public EmailMessage Message { get; set; } = new();
        public int RetryCount { get; set; } = 0;
        public DateTime QueuedAt { get; set; } = DateTime.Now;
        public DateTime? LastAttempt { get; set; }
        public DateTime? NextRetry { get; set; }
        public string? LastError { get; set; }
        public QueueItemStatus Status { get; set; } = QueueItemStatus.Pending;
    }

    public enum QueueItemStatus
    {
        Pending,
        Processing,
        Sent,
        Failed,
        Retrying
    }
}
