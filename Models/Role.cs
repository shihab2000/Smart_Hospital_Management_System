using System.ComponentModel.DataAnnotations;

namespace SHMS.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [StringLength(100)]
        public string RoleName { get; set; } = string.Empty;

        public ICollection<User>? Users { get; set; }
    }
}