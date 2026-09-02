using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        // Links this patient record to a login account (User), so a logged-in
        // patient can view their own records.
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}