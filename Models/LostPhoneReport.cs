using System.ComponentModel.DataAnnotations;

namespace MobileTracker.Models
{
    public class LostPhoneReport
    {
        [Key]
        public int CaseId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Brand { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Model { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Color { get; set; } = string.Empty;

        [Required(ErrorMessage = "IMEI number is required.")]
        [RegularExpression(@"^\d{15}$",
           ErrorMessage = "IMEI must be exactly 15 digits.")]
        public string IMEI { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfLoss { get; set; }

        [Required]
        [StringLength(200)]
        public string LocationLost { get; set; } = string.Empty;


        [Range(0, 100000000,
           ErrorMessage = "Reward amount cannot be negative.")]

        public decimal? RewardAmount { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Submitted";

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SolvedAt { get; set; } 

        public User? User { get; set; }
    }
}