using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SHMS.Models
{
    public class MedicalRecord
    {
        public int MedicalRecordId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [ForeignKey("PatientId")]
        public Patient? Patient { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [ForeignKey("DoctorId")]
        public Doctor? Doctor { get; set; }

        [StringLength(500)]
        public string? Symptoms { get; set; }

        [StringLength(500)]
        public string? Diagnosis { get; set; }

        [StringLength(500)]
        public string? Treatment { get; set; }

        [Display(Name = "Medical Notes")]
        [StringLength(1000)]
        public string? MedicalNotes { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Record Date")]
        public DateTime RecordDate { get; set; }
    }
}