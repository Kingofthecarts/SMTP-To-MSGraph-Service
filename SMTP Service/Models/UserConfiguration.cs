namespace SMTP_Service.Models
{
    public class UserConfiguration
    {
        public List<SmtpCredential> Credentials { get; set; } = new();
    }

    public class SmtpCredential
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
