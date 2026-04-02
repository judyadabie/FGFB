namespace FGFB.Models
{
    public class LeaguePaymentSuccessViewModel
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string LeagueName { get; set; } = "";

        public decimal BaseFee { get; set; }
        public decimal ConvenienceFee { get; set; }
        public decimal TotalFee { get; set; }

        public string LeagueUrl { get; set; } = "";
        public DateTime? PaidUtc { get; set; }
    }
}