using CodeGrade.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace CodeGrade.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        
        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }
        
        public async Task<bool> IsEmailServiceConfiguredAsync()
        {
            return _emailSettings.IsConfigured;
        }
        
        public async Task SendEmailConfirmationAsync(string email, string confirmationLink)
        {
            var subject = "Потвърдете вашия CodeGrade акаунт";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #4F46E5; color: white; padding: 20px; text-align: center;'>
                        <h1 style='margin: 0;'>CodeGrade</h1>
                    </div>
                    <div style='padding: 20px; background-color: #f9fafb;'>
                        <h2 style='color: #374151;'>Добре дошли в CodeGrade!</h2>
                        <p style='color: #6b7280; font-size: 16px;'>
                            Благодарим ви за регистрацията! За да активирате вашия акаунт, 
                            моля потвърдете вашия имейл адрес като кликнете на бутона по-долу:
                        </p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{confirmationLink}' 
                               style='background-color: #4F46E5; color: white; padding: 12px 24px; 
                                      text-decoration: none; border-radius: 6px; display: inline-block; 
                                      font-weight: bold;'>
                                Потвърди имейл
                            </a>
                        </div>
                        <p style='color: #6b7280; font-size: 14px;'>
                            Ако бутонът не работи, можете да копирате и поставите този линк в браузъра:
                        </p>
                        <p style='color: #4F46E5; font-size: 14px; word-break: break-all;'>
                            {confirmationLink}
                        </p>
                        <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;'>
                        <p style='color: #9ca3af; font-size: 12px; text-align: center;'>
                            Ако не сте създали този акаунт, можете да игнорирате този имейл.
                        </p>
                    </div>
                </div>";
            
            await SendEmailAsync(email, subject, body);
        }
        
        public async Task SendPasswordResetAsync(string email, string resetLink)
        {
            var subject = "Нулиране на парола - CodeGrade";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #DC2626; color: white; padding: 20px; text-align: center;'>
                        <h1 style='margin: 0;'>CodeGrade</h1>
                    </div>
                    <div style='padding: 20px; background-color: #f9fafb;'>
                        <h2 style='color: #374151;'>Нулиране на парола</h2>
                        <p style='color: #6b7280; font-size: 16px;'>
                            Получихме заявка за нулиране на паролата за вашия акаунт. 
                            За да продължите, кликнете на бутона по-долу:
                        </p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{resetLink}' 
                               style='background-color: #DC2626; color: white; padding: 12px 24px; 
                                      text-decoration: none; border-radius: 6px; display: inline-block; 
                                      font-weight: bold;'>
                                Нулирай парола
                            </a>
                        </div>
                        <p style='color: #6b7280; font-size: 14px;'>
                            Ако бутонът не работи, можете да копирате и поставите този линк в браузъра:
                        </p>
                        <p style='color: #DC2626; font-size: 14px; word-break: break-all;'>
                            {resetLink}
                        </p>
                        <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;'>
                        <p style='color: #9ca3af; font-size: 12px; text-align: center;'>
                            Ако не сте заявявали нулиране на парола, можете да игнорирате този имейл.
                        </p>
                    </div>
                </div>";
            
            await SendEmailAsync(email, subject, body);
        }
        
        public async Task SendWelcomeEmailAsync(string email, string firstName)
        {
            var subject = "Добре дошли в CodeGrade!";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #059669; color: white; padding: 20px; text-align: center;'>
                        <h1 style='margin: 0;'>CodeGrade</h1>
                    </div>
                    <div style='padding: 20px; background-color: #f9fafb;'>
                        <h2 style='color: #374151;'>Здравейте, {firstName}!</h2>
                        <p style='color: #6b7280; font-size: 16px;'>
                            Вашият акаунт е успешно потвърден и активиран! 
                            Сега можете да влезете в системата и да започнете да използвате CodeGrade.
                        </p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='https://your-domain.com/Account/Login' 
                               style='background-color: #059669; color: white; padding: 12px 24px; 
                                      text-decoration: none; border-radius: 6px; display: inline-block; 
                                      font-weight: bold;'>
                                Влез в системата
                            </a>
                        </div>
                        <p style='color: #6b7280; font-size: 14px;'>
                            Ако имате въпроси или нужда от помощ, не се колебайте да се свържете с нас.
                        </p>
                        <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;'>
                        <p style='color: #9ca3af; font-size: 12px; text-align: center;'>
                            Благодарим ви, че избрахте CodeGrade!
                        </p>
                    </div>
                </div>";
            
            await SendEmailAsync(email, subject, body);
        }
        
        private async Task SendEmailAsync(string to, string subject, string body)
        {
            if (!_emailSettings.IsConfigured)
            {
                _logger.LogWarning("Email service is not configured. Skipping email to {Email}", to);
                return;
            }
            
            try
            {
                using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
                {
                    EnableSsl = _emailSettings.EnableSsl,
                    Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword),
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };
                
                var message = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(to);
                
                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email} with subject: {Subject}", to, subject);
                throw new InvalidOperationException($"Failed to send email to {to}", ex);
            }
        }
    }
}
