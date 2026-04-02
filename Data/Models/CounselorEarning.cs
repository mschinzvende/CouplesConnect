namespace CouplesConnect.Models
{
    public class CounselorEarning
    {
        public int CounselorEarningId { get; set; }

        public Guid CounselorId { get; set; }

        public Guid BookingId { get; set; }

        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}