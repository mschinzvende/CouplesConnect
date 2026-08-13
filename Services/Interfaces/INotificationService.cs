using CouplesConnect.Enums;

using CouplesConnect.Models;

namespace CouplesConnect.Services.Interfaces
{
    public interface INotificationService
    {
        Task NotifyAsync(
            Guid userId,
            NotificationType type,
            string title,
            string message,
            string? link = null,
            bool sendEmail = false,
            bool sendWhatsApp = false);

        Task SendAccountConfirmationAsync(
            ApplicationUser user,
            string confirmationUrl,
            string whatsAppButtonToken);

        Task SendAdministratorCreatedAccountAsync(
            ApplicationUser user,
            string passwordSetupUrl,
            string whatsAppButtonToken,
            string roleSummary);

        Task SendPasswordResetAsync(
            ApplicationUser user,
            string resetUrl,
            string whatsAppButtonToken);

        Task SendPasswordChangedAlertAsync(ApplicationUser user);

        Task NotifyCounselorApplicationSubmittedAsync(Guid counselorUserId);
        Task NotifyCounselorApplicationApprovedAsync(Guid counselorUserId);
        Task NotifyCounselorApplicationRejectedAsync(Guid counselorUserId, string reason);

        Task NotifyBookingCreatedAsync(Guid bookingId);


        Task NotifyCounselorBookingCreatedAsync(Guid bookingId);
        Task NotifyBookingReminderAsync(Guid bookingId);

        Task NotifyInvitationAsync(int participantId);

        Task NotifyInvitationAcceptedAsync(int participantId);

        Task NotifySessionStartedAsync(Guid bookingId);

        Task NotifySessionCompletedAsync(Guid bookingId);
    }
}
