using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using CloudContactManager.Services.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CloudContactManager.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IAmazonSimpleEmailService _sesClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IAmazonSimpleEmailService sesClient,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<NotificationService> logger)
        {
            _sesClient = sesClient;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
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
            var token = _configuration["SpeedSMS:AccessToken"];
            var device = _configuration["SpeedSMS:Device"];

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(device))
            {
                _logger.LogWarning("SpeedSMS configuration is missing. Skipping SMS send.");
                return;
            }

            var client = _httpClientFactory.CreateClient();

            // Basic auth with token as username and ":x" as password per SpeedSMS docs
            var authBytes = Encoding.ASCII.GetBytes($"{token}:x");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var payload = new
            {
                to = new[] { phoneNumber },
                content = message,
                sms_type = 2,
                sender = device
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.speedsms.vn/index.php/sms/send", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("SpeedSMS send failed. StatusCode={StatusCode}, Body={Body}",
                    (int)response.StatusCode, errorBody);
            }
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
                foreach (var phone in recipients)
                {
                    await SendSmsAsync(phone, message);
                }
            }
        }
    }
}