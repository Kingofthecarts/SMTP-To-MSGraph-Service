namespace SMTP_Service.Models
{
    public class EmailAttachment
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public long Size { get; set; }
        public string ContentId { get; set; } = string.Empty; // For inline attachments
        public bool IsInline { get; set; } = false;
    }
}
