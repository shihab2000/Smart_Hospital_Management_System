using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SHMS.Models
{
    public class Invoice
    {
        public int InvoiceId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [ForeignKey("PatientId")]
        public Patient? Patient { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Invoice Date")]
        public DateTime InvoiceDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Consultation Fee")]
        public decimal ConsultationFee { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Medicine Charge")]
        public decimal MedicineCharge { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Lab Charge")]
        public decimal LabCharge { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Required, StringLength(20)]
        [Display(Name = "Payment Status")]
        public string PaymentStatus { get; set; } = "Unpaid";

        public ICollection<Payment>? Payments { get; set; }
    }
}