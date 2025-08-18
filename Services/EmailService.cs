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
        
        public Task<bool> IsEmailServiceConfiguredAsync()
        {
            return Task.FromResult(_emailSettings.IsConfigured);
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
            
            try
            {
                await SendEmailAsync(email, subject, body);
            }
            catch (Exception)
            {
                // Временно решение - показваме линка в конзолата за development
                _logger.LogWarning("Email sending failed, showing confirmation link in console for development: {Link}", confirmationLink);
                Console.WriteLine($"\n=== EMAIL CONFIRMATION LINK (DEVELOPMENT) ===");
                Console.WriteLine($"Email: {email}");
                Console.WriteLine($"Confirmation Link: {confirmationLink}");
                Console.WriteLine($"=============================================\n");
                
                // Прехвърляме грешката нагоре
                throw;
            }
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
                            <a href='https://codegrade.kunev.dev/Account/Login' 
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
            
            _logger.LogInformation("Attempting to send email to {Email} via {Server}:{Port}", 
                to, _emailSettings.SmtpServer, _emailSettings.SmtpPort);
            
            Exception? lastException = null;
            
            // Опитваме с различни портове и настройки
            var configurations = new[]
            {
                new { Port = _emailSettings.SmtpPort, EnableSsl = _emailSettings.EnableSsl, Description = "Primary configuration" },
                new { Port = 587, EnableSsl = true, Description = "Fallback to port 587 with SSL" },
                new { Port = 25, EnableSsl = false, Description = "Fallback to port 25 without SSL" }
            };
            
            foreach (var config in configurations)
            {
                try
                {
                    _logger.LogInformation("Trying {Description}: Port {Port}, SSL: {EnableSsl}", 
                        config.Description, config.Port, config.EnableSsl);
                    
                    using var client = new SmtpClient();
                    client.Host = _emailSettings.SmtpServer;
                    client.Port = config.Port;
                    client.EnableSsl = config.EnableSsl;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
                    client.Timeout = 10000; // 10 секунди timeout
                    
                    var message = new MailMessage
                    {
                        From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };
                    message.To.Add(to);
                    
                    _logger.LogInformation("Sending email message via {Port}...", config.Port);
                    await client.SendMailAsync(message);
                    _logger.LogInformation("Email sent successfully to {Email} via {Port}", to, config.Port);
                    return; // Успех - излизаме от цикъла
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning("Failed to send email via {Port}: {Error}", config.Port, ex.Message);
                    
                    // Ако е последният опит, логваме грешката
                    if (config == configurations.Last())
                    {
                        _logger.LogError(ex, "All email configurations failed for {Email}. Server: {Server}, Username: {Username}", 
                            to, _emailSettings.SmtpServer, _emailSettings.SmtpUsername);
                    }
                }
            }
            
            // Ако всички опити са неуспешни
            throw new InvalidOperationException($"Failed to send email to {to} after trying all configurations", lastException);
        }
    }
}
