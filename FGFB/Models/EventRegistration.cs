using System.ComponentModel.DataAnnotations;

namespace FGFB.Models
{
    public class EventRegistration
    {
        [Key]
        public long EventRegistrationId { get; set; }

        [Required, MaxLength(150)]
        public string EventName { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string LastName { get; set; } = string.Empty;

        [Required, MaxLength(320)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string LeagueLevel { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LeagueDisplayName { get; set; } = string.Empty;

        public decimal BaseTicketPrice { get; set; }
        public decimal LeagueFee { get; set; }
        public decimal ProcessingFee { get; set; }
        public decimal TotalPaid { get; set; }
        public bool AgreeToTerms { get; set; }

        [Required, MaxLength(50)]
        public string PaymentStatus { get; set; } = "Pending";

        [MaxLength(255)]
        public string? StripeSessionId { get; set; }

        [MaxLength(255)]
        public string? StripePaymentIntentId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}