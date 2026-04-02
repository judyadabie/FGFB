namespace FGFB.Models
{
    public class LeaguePaymentSuccessViewModel
    {
        public string LeagueType { get; set; } = "";
        public decimal? EntryFee { get; set; }
        public DateTime? DraftDate { get; set; }
        public string Email { get; set; } = "";
        public string LeagueLink { get; set; } = "";
        public string PaymentStatus { get; set; } = "";
    }
}