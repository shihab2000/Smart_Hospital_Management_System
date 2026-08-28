using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SHMS.Models
{
    public class LabTest
    {
        public int LabTestId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [ForeignKey("PatientId")]
        public Patient? Patient { get; set; }

        [Required, StringLength(150)]
        [Display(Name = "Test Name")]
        public string TestName { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Test Date")]
        public DateTime TestDate { get; set; }

        [Required, StringLength(20)]
        [Display(Name = "Test Status")]
        public string TestStatus { get; set; } = "Pending";

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Test Fee")]
        public decimal TestFee { get; set; }

        public LabReport? LabReport { get; set; }
    }
}