using System.ComponentModel.DataAnnotations;

namespace MobileTracker.Models
{
    public class CaseStatusHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CaseId { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty;

        [Required]
        public DateTime ChangedAt { get; set; }

        // Relationship with Lost Phone Report
        public LostPhoneReport? LostPhoneReport { get; set; }
    }
}