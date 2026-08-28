using System.ComponentModel.DataAnnotations;

namespace SHMS.Models
{
    public class InventoryItem
    {
        public int InventoryItemId { get; set; }

        [Required, StringLength(150)]
        [Display(Name = "Item Name")]
        public string ItemName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        [Display(Name = "Item Type")]
        public string ItemType { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        [Required, StringLength(30)]
        public string Unit { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Minimum Stock")]
        public int MinimumStock { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } = "Available";
    }
}