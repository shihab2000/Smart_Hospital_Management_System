using System.ComponentModel.DataAnnotations;

namespace SHMS.Models
{
    public class Ward
    {
        public int WardId { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Ward Name")]
        public string WardName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Ward Type")]
        public string WardType { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Total Beds")]
        public int TotalBeds { get; set; }

        public ICollection<Bed>? Beds { get; set; }
    }
}