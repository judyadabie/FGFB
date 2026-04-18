namespace FGFB.Models
{
    public class CommishDashboardViewModel
    {
        public List<LeagueRegistrationAdminRow> LeagueRegistrations { get; set; } = new();
        public List<EventRegistrationAdminRow> EventRegistrations { get; set; } = new();
    }

    public class LeagueRegistrationAdminRow
    {
        public long RegistrationId { get; set; }
        public long LeagueId { get; set; }
        public string LeagueName { get; set; } = string.Empty;
        public int? SeasonYear { get; set; }
        public string Email { get; set; } = string.Empty;
        public decimal? EntryFee { get; set; }
        public decimal? ProcessingFee { get; set; }
        public decimal? TotalPaid { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string? LeagueLink { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EventRegistrationAdminRow
    {
        public long EventRegistrationId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string LeagueLevel { get; set; } = string.Empty;
        public string LeagueDisplayName { get; set; } = string.Empty;
        public decimal BaseTicketPrice { get; set; }
        public decimal LeagueFee { get; set; }
        public decimal ProcessingFee { get; set; }
        public decimal TotalPaid { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}