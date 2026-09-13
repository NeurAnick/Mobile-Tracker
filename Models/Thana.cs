using System.ComponentModel.DataAnnotations;

namespace MobileTracker.Models
{
    public class Thana
    {
        [Key]
        public int ThanaId { get; set; }

        [Required]
        [StringLength(100)]
        public string ThanaName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string District { get; set; } = string.Empty;
    }
}