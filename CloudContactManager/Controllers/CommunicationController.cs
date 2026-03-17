using CloudContactManager.Data;
using CloudContactManager.Services.Interfaces;
using CloudContactManager.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudContactManager.Controllers
{
    /// <summary>
    /// Controller for bulk communication operations (SMS/Email to multiple customers).
    /// </summary>
    public class CommunicationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly AppDbContext _context;

        public CommunicationController(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // GET: Communication
        // Displays list of customers with checkboxes to select multiple people
        public IActionResult Index()
        {
            var customers = _context.Customers.OrderBy(c => c.FullName).ToList();
            return View(customers);
        }

        // POST: Communication/Send
        // Receives selected customer IDs and sends communication via INotificationService
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(CommunicationRequest request)
        {
            if (request.CustomerIds == null || !request.CustomerIds.Any())
            {
                TempData["Error"] = "Please choose at least one customer.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(request.MessageContent))
            {
                TempData["Error"] = "Message context can not be blank.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Take customer info from DB based on list of IDs sent up
                var selectedCustomers = await _context.Customers
                    .Where(c => request.CustomerIds.Contains(c.Id))
                    .ToListAsync();

                //List of receivers (Email or SMS)
                List<string> recipients = new List<string>();

                if (request.Type.Equals("Email", StringComparison.OrdinalIgnoreCase))
                {
                    // Take EmailAddress column
                    recipients = selectedCustomers
                        .Where(c => !string.IsNullOrWhiteSpace(c.EmailAddress))
                        .Select(c => c.EmailAddress)
                        .ToList();
                }
                else if (request.Type.Equals("SMS", StringComparison.OrdinalIgnoreCase))
                {
                    // Lấy cột PhoneNumber cho chức năng SMS
                    recipients = selectedCustomers
                        .Where(c => !string.IsNullOrWhiteSpace(c.PhoneNumber))
                        .Select(c => c.PhoneNumber)
                        .ToList();
                }
                else
                {
                    TempData["Error"] = "Invalid communication type. Please select Email or SMS.";
                    return RedirectToAction(nameof(Index));
                }

                // Call bulk sending service
                if (recipients.Any())
                {
                    await _notificationService.SendBulkAsync(recipients, request.MessageContent, request.Type);
                    TempData["Success"] = $"Sent successfully to {recipients.Count} recipients.";
                }
                else
                {
                    TempData["Warning"] = $"No valid {request.Type} addresses found in the selected list.";
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần thiết
                TempData["Error"] = "Error occur: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("api/communication/send")]
        public async Task<IActionResult> SendApi([FromBody] CommunicationRequest request)
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

            await _notificationService.SendBulkAsync(recipients, request.MessageContent, request.Type);
            return Ok(new { Message = $"Sent successfully to {recipients.Count} recipients." });
        }
    }
}