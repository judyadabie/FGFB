using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FGFB.Models
{
    public class ChampionshipDraftWeekendSignupViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string LeagueLevel { get; set; } = "0";

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the Terms and Conditions.")]
        public bool AgreeToTerms { get; set; }

        [ValidateNever]
        public decimal BaseTicketPrice { get; set; }

        [ValidateNever]
        public string PricingLabel { get; set; } = string.Empty;

        [ValidateNever]
        public string? ErrorMessage { get; set; }

        [ValidateNever]
        public bool IsMemberValidated { get; set; }

        [ValidateNever]
        public decimal LeagueFee => LeagueLevel switch
        {
            "100" => 100m,
            "50" => 50m,
            "25" => 25m,
            "0" => 0m,
            _ => 0m
        };

        [ValidateNever]
        public string LeagueDisplayName => LeagueLevel switch
        {
            "100" => "$100 Championship League",
            "50" => "$50 League",
            "25" => "$25 League",
            "0" => "Free Community League",
            _ => "Free Community League"
        };

        [ValidateNever]
        public decimal ProcessingFee => Math.Round(((BaseTicketPrice + LeagueFee) * 0.03m) + 0.80m, 2);

        [ValidateNever]
        public decimal Total => BaseTicketPrice + LeagueFee + ProcessingFee;

        [ValidateNever]
        public decimal FirstPrize100 => 100m * 12m * 0.60m;

        [ValidateNever]
        public decimal SecondPrize100 => 100m * 12m * 0.30m;

        [ValidateNever]
        public decimal FirstPrize50 => 50m * 12m * 0.60m;

        [ValidateNever]
        public decimal SecondPrize50 => 50m * 12m * 0.30m;

        [ValidateNever]
        public decimal FirstPrize25 => 25m * 12m * 0.60m;

        [ValidateNever]
        public decimal SecondPrize25 => 25m * 12m * 0.30m;
    }
}