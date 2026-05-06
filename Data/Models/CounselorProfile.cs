using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CouplesConnect.Models
{
    public class CounselorProfile
    {
        [Key]
        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [Required]
        public string FullName { get; set; }

        public string Bio { get; set; }

        public string Qualifications { get; set; }

        public int YearsOfExperience { get; set; }

        

        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }

        public bool IsApproved { get; set; } = false;

        public bool IsRejected { get; set; } = false; // ✅ NEW

        public string? RejectionReason { get; set; }  // ✅ NEW
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}