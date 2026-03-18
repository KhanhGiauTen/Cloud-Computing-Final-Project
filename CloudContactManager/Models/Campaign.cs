using System.ComponentModel.DataAnnotations;

namespace CloudContactManager.Models
{
    /// <summary>
    /// Marketing campaign created by a tenant for sending SMS/Email.
    /// </summary>
    public class Campaign
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Owning tenant (user) id.
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Message content sent to customers.
        /// </summary>
        [Required]
        public string MessageContent { get; set; } = string.Empty;

        /// <summary>
        /// Communication type: SMS or Email.
        /// </summary>
        [Required]
        public string CommunicationType { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }

        public ICollection<CommunicationLog> CommunicationLogs { get; set; } = new List<CommunicationLog>();
    }
}
