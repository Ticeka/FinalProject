using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace FinalProject.Services // ตั้ง namespace ให้ตรงกับโปรเจกต์คุณ
{
    public class NullEmailSender : IEmailSender
    {
        private readonly ILogger<NullEmailSender> _logger;

        public NullEmailSender(ILogger<NullEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Log เพื่อ debug (ไม่ได้ส่งจริง)
            _logger.LogInformation("Mock email sent to {Email} | Subject: {Subject}", email, subject);
            return Task.CompletedTask;
        }
    }
}
