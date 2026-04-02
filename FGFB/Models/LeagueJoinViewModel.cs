using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FGFB.Models
{
    public class LeagueJoinViewModel
    {
        [ValidateNever]
        public long LeagueId { get; set; }

        [ValidateNever]
        public string? LeagueType { get; set; }

        [ValidateNever]
        public decimal? EntryFee { get; set; }

        [ValidateNever]
        public DateTime? DraftDate { get; set; }

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string? Email { get; set; }

        [ValidateNever]
        public bool EmailValidated { get; set; }

        [ValidateNever]
        public string? ErrorMessage { get; set; }
    }
}