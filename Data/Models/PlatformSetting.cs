using System.ComponentModel.DataAnnotations;

namespace CouplesConnect.Models
{
    public class PlatformSetting
    {
        [Key]
        public int Id { get; set; }

        #region General

        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Display(Name = "Company Email")]
        public string? CompanyEmail { get; set; }

        [Display(Name = "Company Phone")]
        public string? CompanyPhone { get; set; }

        public string Currency { get; set; } = "USD";

        public string TimeZone { get; set; } = "Africa/Harare";

        #endregion

        #region Subscription

        public int TrialSessions { get; set; }

        public int TrialDurationDays { get; set; }

        public decimal CounselorCommissionPercent { get; set; }

        #endregion

        #region WhatsApp

        public string? WhatsAppApiUrl { get; set; }

        public string? WhatsAppAccessToken { get; set; }

        public string? WhatsAppPhoneNumberId { get; set; }

        public string? WhatsAppBusinessAccountId { get; set; }

        public string? WhatsAppVerifyToken { get; set; }

        #endregion

        #region Email

        public string? SmtpServer { get; set; }

        public int SmtpPort { get; set; }

        public string? SmtpUsername { get; set; }

        public string? SmtpPassword { get; set; }

        public bool EnableSSL { get; set; } = true;

        #endregion

        #region Payments

        public string? PaynowIntegrationId { get; set; }

        public string? PaynowIntegrationKey { get; set; }

        #endregion

        public string? BaseUrl { get; set; } = "";

        public bool NotifyCounsellorOnSessionStart { get; set; } = true;
    }
}