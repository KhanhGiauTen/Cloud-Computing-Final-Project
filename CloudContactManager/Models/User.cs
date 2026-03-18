using System.ComponentModel.DataAnnotations;

namespace CloudContactManager.Models
{
    /// <summary>
    /// Application user (tenant owner) entity.
    /// Each user manages their own set of customers.
    /// </summary>
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Customers owned by this user (tenant scope).
        /// </summary>
        public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    }
}
