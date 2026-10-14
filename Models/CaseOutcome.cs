using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MobileTracker.Models
{
    [Index(nameof(CaseId), IsUnique = true)]
    public class CaseOutcome
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CaseId { get; set; }

        [Required]
        [StringLength(50)]
        public string Outcome { get; set; } = string.Empty;

        public DateTime? RecordedAt { get; set; }

        public LostPhoneReport? LostPhoneReport { get; set; }
    }
}