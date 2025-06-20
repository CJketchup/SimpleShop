using Microsoft.AspNetCore.Identity.UI.Services; // 這一行是關鍵
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SimpleShop.Services
{
    public class EmailSender : IEmailSender // 這裡的 IEmailSender 會解析為 Microsoft.AspNetCore.Identity.UI.Services.IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly SmtpSettings _smtpSettings;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
            _smtpSettings = _configuration.GetSection("SmtpSettings").Get<SmtpSettings>()
                            ?? new SmtpSettings();
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // ... (郵件發送邏輯不變) ...
            Debug.WriteLine($"-------------------- ATTEMPTING TO SEND EMAIL --------------------");
            Debug.WriteLine($"To: {email}");
            Debug.WriteLine($"Subject: {subject}");
            Debug.WriteLine($"Message: (HTML)");
            Debug.WriteLine($"SMTP Host: {_smtpSettings.Server}");
            Debug.WriteLine($"SMTP Port: {_smtpSettings.Port}");
            Debug.WriteLine($"SMTP User: {_smtpSettings.Username}");
            Debug.WriteLine($"------------------------------------------------------------------");

            if (string.IsNullOrEmpty(_smtpSettings.Server) || string.IsNullOrEmpty(_smtpSettings.Username) || string.IsNullOrEmpty(_smtpSettings.FromAddress))
            {
                Debug.WriteLine("SMTP settings (Server, Username, or FromAddress) are not configured or incomplete. Email not sent.");
                return Task.CompletedTask;
            }

            try
            {
                var client = new SmtpClient(_smtpSettings.Server, _smtpSettings.Port)
                {
                    Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                    EnableSsl = _smtpSettings.EnableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpSettings.FromAddress, _smtpSettings.FromName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(email);

                return client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending email: {ex.Message}");
                // 實際應用中應記錄此錯誤
                return Task.FromException(ex);
            }
        }
    }

    public class SmtpSettings
    {
        public string Server { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string FromName { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }
}