using System.ComponentModel.DataAnnotations;

namespace SHMS.Models
{
    public class Patient
    {
        public int PatientId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Required, StringLength(20)]
        public string Gender { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        [Display(Name = "Blood Group")]
        [StringLength(5)]
        public string? BloodGroup { get; set; }

        [Display(Name = "Emergency Contact")]
        [Phone]
        public string? EmergencyContact { get; set; }

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}