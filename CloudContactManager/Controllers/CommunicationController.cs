using CloudContactManager.Data;
using CloudContactManager.Services.Interfaces;
using CloudContactManager.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

            var selectedCustomers = await _context.Customers
                .Where(c => request.CustomerIds.Contains(c.Id))
                .ToListAsync();

            List<string> recipients;

            if (request.Type.Equals("Email", StringComparison.OrdinalIgnoreCase))
            {
                recipients = selectedCustomers
                    .Where(c => !string.IsNullOrWhiteSpace(c.EmailAddress))
                    .Select(c => c.EmailAddress)
                    .ToList();
            }
            else if (request.Type.Equals("SMS", StringComparison.OrdinalIgnoreCase))
            {
                recipients = selectedCustomers
                    .Where(c => !string.IsNullOrWhiteSpace(c.PhoneNumber))
                    .Select(c => c.PhoneNumber)
                    .ToList();
            }
            else
            {
                return BadRequest(new { Message = "Invalid communication type. Use Email or SMS." });
            }

            if (!recipients.Any())
            {
                return NotFound(new { Message = "No valid recipients found in the selected list." });
            }

            try
            {
                await _notificationService.SendBulkAsync(recipients, request.MessageContent, request.Type);
                return Ok(new { Message = $"Sent successfully to {recipients.Count} recipients." });
            }
            catch (Exception ex)
            {
                // Trả về lỗi server 500 nếu quá trình gửi gặp trục trặc từ AWS/SpeedSMS
                return StatusCode(500, new { Message = "Error occurred while sending: " + ex.Message });
            }
        }
    }
}