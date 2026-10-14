
using System;
using System.ComponentModel.DataAnnotations;

namespace MobileTracker.Models
{
    public class CaseMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CaseId { get; set; }

        public int? FoundReportId { get; set; }

        [Required]
        public int SenderId { get; set; }

        [Required]
        public int ReceiverId { get; set; }

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public int? MatchId { get; set; }
    }
}
