namespace FGFB.Models
{
    public class LeaguePaymentOptions
    {
        public decimal BaseLeagueFee { get; set; }
        public decimal ConvenienceFeePercent { get; set; }
        public string SuccessUrl { get; set; } = "";
        public string CancelUrl { get; set; } = "";
    }
}