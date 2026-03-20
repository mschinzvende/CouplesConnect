namespace CouplesConnect.Models
{
    public class PlatformSetting
    {
        public int PlatformSettingId { get; set; }

        public int TrialSessions { get; set; } = 2;

        public int TrialDurationDays { get; set; } = 30;

        public int CounselorCommissionPercent { get; set; } = 70;
    }
}