using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using CloudContactManager.Services.Interfaces;

namespace CloudContactManager.Services
{
    public class EmailNotificationService : INotificationService
    {
        private readonly IAmazonSimpleEmailService _sesClient;
        private readonly IConfiguration _configuration;

        public EmailNotificationService(
            IAmazonSimpleEmailService sesClient,
            IConfiguration configuration)
        {
            _sesClient = sesClient;
            _configuration = configuration;
        }

        /// <summary>
        /// Send single email using AWS SES
        /// </summary>
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
            catch (MessageRejectedException ex)
            {
                throw new Exception($"Sandbox Error: Email '{toEmail}' hasn't verified. Detail: {ex.Message}");
            }
            catch (LimitExceededException ex)
            {
                throw new Exception($"Limit Error: AWS SES quota exceeded. Detail: {ex.Message}");
            }
            catch (AmazonServiceException ex)
            {
                throw new Exception($"AWS SES Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Unknown Error: {ex.Message}");
            }
        }

        /// <summary>
        /// SMS not supported in this service (use SNS service instead)
        /// </summary>
        public Task SendSmsAsync(string phoneNumber, string message)
        {
            throw new NotImplementedException("SMS is handled by SNSNotificationService.");
        }

        /// <summary>
        /// Send bulk email to multiple recipients
        /// </summary>
        public async Task SendBulkAsync(List<string> recipients, string message, string type)
        {
            if (type != "Email")
            {
                throw new Exception("EmailNotificationService only supports Email type.");
            }

            var subject = "Notification from Cloud Contact Manager";

            foreach (var email in recipients)
            {
                await SendEmailAsync(email, subject, message);
            }
        }
    }
}