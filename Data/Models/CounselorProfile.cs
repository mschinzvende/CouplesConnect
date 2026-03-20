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

        public string Specializations { get; set; }

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}