using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SHMS.Models
{
    public class Medicine
    {
        public int MedicineId { get; set; }

        [Required, StringLength(150)]
        [Display(Name = "Medicine Name")]
        public string MedicineName { get; set; } = string.Empty;

        [Required]
        public int SupplierId { get; set; }

        [ForeignKey("SupplierId")]
        public Supplier? Supplier { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Expiry Date")]
        public DateTime ExpiryDate { get; set; }

        public ICollection<PrescriptionItem>? PrescriptionItems { get; set; }
        public ICollection<Purchase>? Purchases { get; set; }
        public ICollection<Sale>? Sales { get; set; }
    }
}