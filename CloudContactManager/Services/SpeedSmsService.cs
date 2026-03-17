using CloudContactManager.Services.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CloudContactManager.Services
{
    public class SpeedSmsService : INotificationService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly ILogger<SpeedSmsService> _logger; // Khai báo Logger để hết lỗi CS0103

        public SpeedSmsService(IConfiguration config, ILogger<SpeedSmsService> logger)
        {
            _config = config;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                var apiKey = _config["SpeedSMS:ApiKey"];
                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:"));
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

                var content = new StringContent(JsonConvert.SerializeObject(new
                {
                    to = new[] { phoneNumber },
                    content = message,
                    sms_type = 5,
                    sender = ""
                }), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://api.speedsms.vn/index.php/sms/send", content);
                _logger.LogInformation("SpeedSMS Response: {StatusCode}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi SMS qua SpeedSMS");
            }
        }

        public async Task SendBulkAsync(List<string> phoneNumbers, string message, string sender = null)
        {
            foreach (var phone in phoneNumbers)
            {
                await SendSmsAsync(phone, message);
            }
        }

        public async Task SendEmailAsync(string email, string subject, string body)
        {
            _logger.LogInformation("Gửi Email giả lập tới: {email} với tiêu đề: {subject}", email, subject);
            await Task.CompletedTask;
        }
    }
}