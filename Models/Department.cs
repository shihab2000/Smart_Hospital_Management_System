using System.ComponentModel.DataAnnotations;

namespace SHMS.Models
{
    public class Department
    {
        public int DepartmentId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public ICollection<Doctor>? Doctors { get; set; }
    }
}