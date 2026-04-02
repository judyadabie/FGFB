using System.ComponentModel.DataAnnotations;

namespace FGFB.Models
{
    public class LeagueSignupViewModel
    {
        [Required]
        public string FirstName { get; set; } = "";

        [Required]
        public string LastName { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Phone]
        public string? Phone { get; set; }

        [Required]
        public string LeagueName { get; set; } = "Fan Girl Football League";

        public decimal BaseFee { get; set; }
        public decimal ConvenienceFee { get; set; }
        public decimal TotalFee { get; set; }
    }
}