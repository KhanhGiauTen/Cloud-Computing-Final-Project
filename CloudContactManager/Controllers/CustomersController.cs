using CloudContactManager.Data;
using CloudContactManager.Models;
using CloudContactManager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CloudContactManager.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController] // Bắt buộc cho API
    public class CustomersController : ControllerBase // Đổi từ Controller sang ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public CustomersController(AppDbContext context, INotificationService notificationService)
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

        // GET: api/Customers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            var currentUserId = GetCurrentUserId();

            var customers = await _context.Customers
                .Where(c => c.UserId == currentUserId)
                .ToListAsync();
            return Ok(customers); // Trả về list dạng JSON
        }

        // GET: api/Customers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            var currentUserId = GetCurrentUserId();

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == currentUserId);

            if (customer == null)
            {
                return NotFound(new { Message = "Không tìm thấy khách hàng." });
            }

            return Ok(customer);
        }

        // POST: api/Customers
        [HttpPost]
        public async Task<ActionResult<Customer>> CreateCustomer([FromBody] Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUserId = GetCurrentUserId();

            // Gán ngày tạo và user sở hữu bản ghi
            customer.CreatedAt = DateTime.UtcNow;
            customer.UserId = currentUserId;

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            // (Tuỳ chọn) TODO: Gọi _notificationService gửi email chào mừng ở đây

            // Trả về code 201 Created và thông tin customer vừa tạo
            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
        }

        // PUT: api/Customers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var currentUserId = GetCurrentUserId();

            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == currentUserId);

            if (existingCustomer == null)
            {
                return NotFound(new { Message = "Không tìm thấy khách hàng." });
            }

            if (existingCustomer.UserId != currentUserId)
            {
                return Forbid();
            }

            // Cập nhật các trường cho phép chỉnh sửa, không cho đổi UserId
            existingCustomer.FullName = customer.FullName;
            existingCustomer.Address = customer.Address;
            existingCustomer.PhoneNumber = customer.PhoneNumber;
            existingCustomer.EmailAddress = customer.EmailAddress;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Customers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var currentUserId = GetCurrentUserId();

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == currentUserId);
            if (customer == null)
            {
                return NotFound(new { Message = "Không tìm thấy khách hàng." });
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đã xóa khách hàng thành công." });
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.Id == id);
        }
    }
}