using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using CloudContactManager.Services.Interfaces;
using CloudContactManager.Services.API;

namespace CloudContactManager.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IAmazonSimpleEmailService _sesClient;
        private readonly IConfiguration _configuration;

        public NotificationService(
            IAmazonSimpleEmailService sesClient,
            IConfiguration configuration)
        {
            _sesClient = sesClient;
            _configuration = configuration;
        }

        // ================= EMAIL (AWS SES) =================
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var sender = _configuration["AWS:SenderEmail"];

            var request = new SendEmailRequest
            {
                Source = sender,
                Destination = new Destination
                {
                    ToAddresses = new List<string> { toEmail }
                },
                Message = new Message
                {
                    Subject = new Content(subject),
                    Body = new Body
                    {
                        Html = new Content(body)
                    }
                }
            };

            try
            {
                await _sesClient.SendEmailAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception($"Email Error: {ex.Message}");
            }
        }

        // ================= SMS (SpeedSMS) =================
        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            string token = _configuration["SpeedSMS:AccessToken"];
            string sender = _configuration["SpeedSMS:Device"];

            var api = new SpeedSMSAPI(token);

            string[] phones = new string[] { phoneNumber };

            int type = SpeedSMSAPI.TYPE_GATEWAY;

            string response = api.sendSMS(phones, message, type, sender);

            await Task.CompletedTask;
        }

        // ================= BULK =================
        public async Task SendBulkAsync(List<string> recipients, string message, string type)
        {
            if (type.Equals("Email", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var email in recipients)
                {
                    await SendEmailAsync(email, "Notification", message);
                }
            }
            else if (type.Equals("SMS", StringComparison.OrdinalIgnoreCase))
            {
                string token = _configuration["SpeedSMS:AccessToken"];
                string sender = _configuration["SpeedSMS:Device"];

                var api = new SpeedSMSAPI(token);

                string[] phones = recipients.ToArray();

                int smsType = SpeedSMSAPI.TYPE_GATEWAY;

                string response = api.sendSMS(phones, message, smsType, sender);
            }
        }
    }
}