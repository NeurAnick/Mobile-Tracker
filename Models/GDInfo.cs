using System.ComponentModel.DataAnnotations;

namespace MobileTracker.Models
{
    public class GDInfo
    {
        [Key]
        public int GDInfoId { get; set; }

        [Required]
        [StringLength(50)]
        public string GDNumber { get; set; } = string.Empty;

        [Required]
        public int ThanaId { get; set; }

        public string? GDPhotoPath { get; set; }

        [Required]
        public int CaseId { get; set; }

        public Thana? Thana { get; set; }

        public LostPhoneReport? LostPhoneReport { get; set; }
    }
}