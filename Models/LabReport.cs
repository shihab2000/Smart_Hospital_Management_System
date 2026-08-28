using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SHMS.Models
{
    public class LabReport
    {
        public int LabReportId { get; set; }

        [Required]
        public int LabTestId { get; set; }

        [ForeignKey("LabTestId")]
        public LabTest? LabTest { get; set; }

        [Required, StringLength(1000)]
        public string Result { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Remarks { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Report Date")]
        public DateTime ReportDate { get; set; }
    }
}