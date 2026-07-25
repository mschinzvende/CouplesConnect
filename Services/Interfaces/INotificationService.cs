using CouplesConnect.Enums;

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

        Task NotifyBookingCreatedAsync(Guid bookingId);

        Task NotifyBookingReminderAsync(Guid bookingId);

        Task NotifyInvitationAsync(int participantId);

        Task NotifyInvitationAcceptedAsync(int participantId);

        Task NotifySessionStartedAsync(Guid bookingId);

        Task NotifySessionCompletedAsync(Guid bookingId);
    }
}