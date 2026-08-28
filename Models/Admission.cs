using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SHMS.Models
{
    public class Admission
    {
        public int AdmissionId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [ForeignKey("PatientId")]
        public Patient? Patient { get; set; }

        [Required]
        public int WardId { get; set; }

        [ForeignKey("WardId")]
        public Ward? Ward { get; set; }

        [Required]
        public int BedId { get; set; }

        [ForeignKey("BedId")]
        public Bed? Bed { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Admission Date")]
        public DateTime AdmissionDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Discharge Date")]
        public DateTime? DischargeDate { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } = "Admitted";
    }
}