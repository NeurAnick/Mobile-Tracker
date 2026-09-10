using System.ComponentModel.DataAnnotations;

namespace MobileTracker.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [RegularExpression(@"^[a-z0-9._%+-]+@(gmail\.com)$",
            ErrorMessage = "Please enter a valid email address.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile number is required.")]
        [RegularExpression(@"^01[3-9]\d{8}$",
            ErrorMessage = "Please enter a valid Bangladeshi mobile number.")]
        public string MobileNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string PasswordHash { get; set; } = string.Empty;

        public bool EmailConfirmed { get; set; } = false;

        public string? EmailVerificationToken { get; set; }
    }
}