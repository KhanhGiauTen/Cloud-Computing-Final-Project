using CloudContactManager.Data;
using CloudContactManager.Models;
using CloudContactManager.Services.Interfaces;
using CloudContactManager.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CloudContactManager.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController] // Bắt buộc khai báo cho Web API
    public class CommunicationController : ControllerBase // Đổi từ Controller sang ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly AppDbContext _context;

        public CommunicationController(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new UnauthorizedAccessException("User id claim is missing.");
            }

            if (!int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User id claim is invalid.");
            }

            return userId;
        }

        // POST: api/Communication/Send
        [HttpPost("Send")]
        public async Task<IActionResult> Send([FromBody] CommunicationRequest request)
        {
            if (request.CustomerIds == null || !request.CustomerIds.Any())
            {
                return BadRequest(new { Message = "Please choose at least one customer." });
            }

            if (string.IsNullOrWhiteSpace(request.MessageContent))
            {
                return BadRequest(new { Message = "Message content can not be blank." });
            }

            var currentUserId = GetCurrentUserId();

            var selectedCustomers = await _context.Customers
                .Where(c => c.UserId == currentUserId && request.CustomerIds.Contains(c.Id))
                .ToListAsync();

            try
            {
                // Tạo campaign cho lần gửi này
                var campaign = new Campaign
                {
                    UserId = currentUserId,
                    MessageContent = request.MessageContent,
                    CommunicationType = request.Type,
                    SentAt = DateTime.UtcNow
                };

                _context.Campaigns.Add(campaign);
                await _context.SaveChangesAsync();

                var logs = new List<CommunicationLog>();
                var successCount = 0;

                foreach (var customer in selectedCustomers)
                {
                    var log = new CommunicationLog
                    {
                        CampaignId = campaign.Id,
                        CustomerId = customer.Id,
                        DeliveryStatus = "Pending"
                    };

                    try
                    {
                        if (request.Type.Equals("Email", StringComparison.OrdinalIgnoreCase))
                        {
                            if (string.IsNullOrWhiteSpace(customer.EmailAddress))
                            {
                                log.DeliveryStatus = "Failed";
                                log.ErrorMessage = "Missing email address";
                            }
                            else
                            {
                                await _notificationService.SendEmailAsync(customer.EmailAddress, "Notification", request.MessageContent);
                                log.DeliveryStatus = "Success";
                                successCount++;
                            }
                        }
                        else if (request.Type.Equals("SMS", StringComparison.OrdinalIgnoreCase))
                        {
                            if (string.IsNullOrWhiteSpace(customer.PhoneNumber))
                            {
                                log.DeliveryStatus = "Failed";
                                log.ErrorMessage = "Missing phone number";
                            }
                            else
                            {
                                await _notificationService.SendSmsAsync(customer.PhoneNumber, request.MessageContent);
                                log.DeliveryStatus = "Success";
                                successCount++;
                            }
                        }
                        else
                        {
                            log.DeliveryStatus = "Failed";
                            log.ErrorMessage = "Invalid communication type";
                        }
                    }
                    catch (Exception ex)
                    {
                        log.DeliveryStatus = "Failed";
                        log.ErrorMessage = ex.Message;
                    }

                    logs.Add(log);
                }

                if (logs.Count > 0)
                {
                    _context.CommunicationLogs.AddRange(logs);
                    await _context.SaveChangesAsync();
                }

                if (successCount == 0)
                {
                    return StatusCode(500, new { Message = "No messages were sent successfully." });
                }

                return Ok(new { Message = $"Sent successfully to {successCount} recipients.", CampaignId = campaign.Id });
            }
            catch (Exception ex)
            {
                // Trả về lỗi server 500 nếu quá trình gửi gặp trục trặc từ AWS/SpeedSMS
                return StatusCode(500, new { Message = "Error occurred while sending: " + ex.Message });
            }
        }
    }
}