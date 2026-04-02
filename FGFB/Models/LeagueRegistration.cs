using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FGFB.Models
{
    [Table("LeagueRegistrations")]
    public class LeagueRegistration
    {
        [Key]
        public long RegistrationId { get; set; }

        public long LeagueId { get; set; }
        public string Email { get; set; } = "";
        public string? LeagueLink { get; set; }

        public string PaymentStatus { get; set; } = "";
        public string? StripeSessionId { get; set; }
        public string? StripePaymentIntentId { get; set; }

        public decimal? EntryFee { get; set; }
        public decimal? ProcessingFee { get; set; }
        public decimal? TotalPaid { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}