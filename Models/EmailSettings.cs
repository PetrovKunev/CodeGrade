namespace CodeGrade.Models
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
        public bool IsConfigured => !string.IsNullOrEmpty(SmtpServer) && 
                                   !string.IsNullOrEmpty(SmtpUsername) && 
                                   !string.IsNullOrEmpty(SmtpPassword);
    }
}
