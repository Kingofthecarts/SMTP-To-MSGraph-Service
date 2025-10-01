namespace SMTP_Service.Models
{
    public class EmailMessage
    {
        public string From { get; set; } = string.Empty;
        public List<string> To { get; set; } = new();
        public List<string> Cc { get; set; } = new();
        public List<string> Bcc { get; set; } = new();
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = false;
        public DateTime ReceivedAt { get; set; } = DateTime.Now;
        public string RawMessage { get; set; } = string.Empty;
    }
}
