using System.ComponentModel.DataAnnotations;

namespace CouplesConnect.Data.Models
{
    public class CounselorService
    {
        [Key]
        public int Id { get; set; }  // avoids composite key config

        public Guid CounselorId { get; set; }

        public int ServiceId { get; set; }

        // Optional extra fields later:
        // public decimal Price { get; set; }
    }
}
