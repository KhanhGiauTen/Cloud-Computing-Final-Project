using System.ComponentModel.DataAnnotations;

namespace CloudContactManager.Models
{
    /// <summary>
    /// Log entry for a single communication attempt in a campaign.
    /// </summary>
    public class CommunicationLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CampaignId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        /// <summary>
        /// Delivery status: Success / Failed.
        /// </summary>
        [Required]
        public string DeliveryStatus { get; set; } = string.Empty;

        /// <summary>
        /// External message id returned by AWS SES / SpeedSMS.
        /// </summary>
        public string? ExternalId { get; set; }

        /// <summary>
        /// Error detail if sending failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        public Campaign? Campaign { get; set; }

        public Customer? Customer { get; set; }
    }
}
