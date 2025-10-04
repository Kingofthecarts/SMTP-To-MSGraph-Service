namespace SMTP_Service.Models
{
    public class SmtpConfiguration
    {
        public int Port { get; set; } = 25;
        public string BindAddress { get; set; } = "0.0.0.0";
        public bool RequireAuthentication { get; set; } = true;
        public int MaxMessageSizeKb { get; set; } = 51200;
        public bool EnableTls { get; set; } = false;
        public bool SmtpFlowEnabled { get; set; } = true;
        public int SendDelayMs { get; set; } = 1000;
    }
}
