namespace CodeGrade.Services
{
    public interface IEmailService
    {
        Task SendEmailConfirmationAsync(string email, string confirmationLink);
        Task SendPasswordResetAsync(string email, string resetLink);
        Task SendWelcomeEmailAsync(string email, string firstName);
        Task<bool> IsEmailServiceConfiguredAsync();
    }
}
