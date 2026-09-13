using System.ComponentModel.DataAnnotations;

namespace MobileTracker.Models
{
    public class PolicyAcceptance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(20)]
        public string PolicyVersion { get; set; } = string.Empty;

        [Required]
        public DateTime AcceptedAt { get; set; }
    }
}