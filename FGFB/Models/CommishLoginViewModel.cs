using System.ComponentModel.DataAnnotations;

namespace FGFB.Models
{
    public class CommishLoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
