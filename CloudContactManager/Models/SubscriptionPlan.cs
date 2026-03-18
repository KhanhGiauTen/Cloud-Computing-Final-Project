using System.ComponentModel.DataAnnotations;

namespace CloudContactManager.Models
{
    /// <summary>
    /// Subscription plan for tenants (Free, Premium, etc.).
    /// </summary>
    public class SubscriptionPlan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string PlanName { get; set; } = string.Empty;

        /// <summary>
        /// Maximum number of customers allowed for this plan.
        /// </summary>
        public int MaxCustomers { get; set; }

        /// <summary>
        /// Monthly price for the plan.
        /// </summary>
        public decimal Price { get; set; }

        public ICollection<User> Tenants { get; set; } = new List<User>();
    }
}
