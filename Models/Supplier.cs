using System.ComponentModel.DataAnnotations;

namespace SHMS.Models
{
    public class Supplier
    {
        public int SupplierId { get; set; }

        [Required, StringLength(150)]
        [Display(Name = "Supplier Name")]
        public string SupplierName { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        public ICollection<Medicine>? Medicines { get; set; }
        public ICollection<Purchase>? Purchases { get; set; }
    }
}