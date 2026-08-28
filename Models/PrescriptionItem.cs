using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SHMS.Models
{
    public class PrescriptionItem
    {
        public int PrescriptionItemId { get; set; }

        [Required]
        public int PrescriptionId { get; set; }

        [ForeignKey("PrescriptionId")]
        public Prescription? Prescription { get; set; }

        [Required]
        public int MedicineId { get; set; }

        [ForeignKey("MedicineId")]
        public Medicine? Medicine { get; set; }

        [Required, StringLength(100)]
        public string Dosage { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Duration { get; set; }

        [StringLength(300)]
        public string? Instructions { get; set; }
    }
}