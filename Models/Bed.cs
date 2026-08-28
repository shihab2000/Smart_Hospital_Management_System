using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SHMS.Models
{
    public class Bed
    {
        public int BedId { get; set; }

        [Required]
        public int WardId { get; set; }

        [ForeignKey("WardId")]
        public Ward? Ward { get; set; }

        [Required, StringLength(20)]
        [Display(Name = "Bed Number")]
        public string BedNumber { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Status { get; set; } = "Available";

        public ICollection<Admission>? Admissions { get; set; }
    }
}