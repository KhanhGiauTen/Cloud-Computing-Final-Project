using CloudContactManager.Services.Interfaces;
using CloudContactManager.Services.API;
using Microsoft.Extensions.Configuration;

namespace CloudContactManager.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IConfiguration _configuration;

        public NotificationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            // Lấy token từ appsettings.json
            string token = _configuration["SpeedSMS:AccessToken"];
            var api = new SpeedSMSAPI(token);

            string[] phones = new string[] { phoneNumber };

            // Tham số type: 1 (QC), 2 (CSKH), 5 (Dùng Android)
            int type = SpeedSMSAPI.TYPE_GATEWAY;
            string sender = _configuration["SpeedSMS:Device"]; 

            // Gọi hàm sendSMS của API
            string response = api.sendSMS(phones, message, type, sender);

            // Bạn có thể log biến 'response' để kiểm tra kết quả trả về từ SpeedSMS
            await Task.CompletedTask;
        }

        public async Task SendBulkAsync(List<string> recipients, string message, string type)
        {
            if (type.Equals("SMS", StringComparison.OrdinalIgnoreCase))
            {
                string token = _configuration["SpeedSMS:AccessToken"];
                var api = new SpeedSMSAPI(token);

                string[] phones = recipients.ToArray();
                int smsType = SpeedSMSAPI.TYPE_GATEWAY;
                string sender = _configuration["SpeedSMS:Device"];

                // Gọi hàm sendSMS cho mảng số điện thoại
                string response = api.sendSMS(phones, message, smsType, sender);
            }

            await Task.CompletedTask;
        }
    }
}