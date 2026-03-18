using System.ComponentModel.DataAnnotations;

namespace CloudContactManager.Models
{
    /// <summary>
    /// Customer entity model.
    /// Properties match assignment requirements: Id, FullName, Address, PhoneNumber, EmailAddress.
    /// </summary>
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;


        public string? Address { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không hợp lệ")]
        [Display(Name = "Email")]
        public string EmailAddress { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Foreign key to the owning user (tenant).
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Navigation to the owning user.
        /// </summary>
        public User? User { get; set; }
    }
}