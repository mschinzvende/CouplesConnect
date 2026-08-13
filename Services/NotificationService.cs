using CouplesConnect.Data;
using CouplesConnect.Data.Models;
using CouplesConnect.Enums;
using CouplesConnect.Models;
using CouplesConnect.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly MessagingService _messaging;
        private readonly EmailTemplateService _templates;
        private readonly SettingsService _settings;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationService(
            ApplicationDbContext context,
            MessagingService messaging,
            EmailTemplateService templates,
            SettingsService settings,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _messaging = messaging;
            _templates = templates;
            _settings = settings;
            _userManager = userManager;
        }

        public async Task NotifyAsync(
    Guid userId,
    NotificationType type,
    string title,
    string message,
    string? link = null,
    bool sendEmail = false,
    bool sendWhatsApp = false)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
                return;

            await CreateNotification(
                userId,
                type,
                title,
                message,
                link);

            if (sendEmail && !string.IsNullOrWhiteSpace(user.Email))
            {
                await SendEmail(
                    user,
                    type,
                    title,
                    message);
            }

            if (sendWhatsApp && !string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                await SendWhatsApp(
                    user,
                    type,
                    message);
            }

            await _context.SaveChangesAsync();
        }

        public async Task SendAccountConfirmationAsync(
            ApplicationUser user,
            string confirmationUrl,
            string whatsAppButtonToken)
        {
            const string title = "Confirm your Couples Connect account";
            const string message = "Please confirm your email address to activate your Couples Connect account.";

            await CreateNotification(user.Id, NotificationType.AccountConfirmation, title, message, confirmationUrl);

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var email = await _templates.BuildAccountConfirmationAsync(user.FullName, confirmationUrl);
                await SendEmail(user, NotificationType.AccountConfirmation, email.Subject, message, email);
            }

            if (!string.IsNullOrWhiteSpace(user.Phone))
            {
                var log = CreateLog(
                    NotificationChannel.WhatsApp,
                    NotificationType.AccountConfirmation,
                    user.Id,
                    null,
                    user.Phone,
                    title,
                    message);

                try
                {
                    var sent = await _messaging.SendWhatsAppAccountConfirmationAsync(
                        user.Phone,
                        user.FullName,
                        whatsAppButtonToken);

                    log.Status = sent ? "Sent" : "Failed";
                    if (sent)
                        log.SentDate = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    log.Status = "Failed";
                    log.ErrorMessage = ex.Message;
                }

                _context.NotificationLogs.Add(log);
            }

            await _context.SaveChangesAsync();
        }

        public async Task SendAdministratorCreatedAccountAsync(
            ApplicationUser user,
            string passwordSetupUrl,
            string whatsAppButtonToken,
            string roleSummary)
        {
            const string title = "Your Couples Connect account is ready";
            const string message = "An administrator created your account. Choose a password to complete your sign-in setup.";

            await CreateNotification(user.Id, NotificationType.AccountCreated, title, message, passwordSetupUrl);

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var email = await _templates.BuildAdministratorCreatedAccountAsync(user.FullName, passwordSetupUrl, roleSummary);
                await SendEmail(user, NotificationType.AccountCreated, email.Subject, message, email);
            }

            if (!string.IsNullOrWhiteSpace(user.Phone))
            {
                var log = CreateLog(
                    NotificationChannel.WhatsApp,
                    NotificationType.AccountCreated,
                    user.Id,
                    null,
                    user.Phone,
                    title,
                    message);

                try
                {
                    var sent = await _messaging.SendWhatsAppAdministratorCreatedAccountAsync(
                        user.Phone,
                        user.FullName,
                        roleSummary,
                        whatsAppButtonToken);

                    log.Status = sent ? "Sent" : "Failed";
                    if (sent)
                        log.SentDate = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    log.Status = "Failed";
                    log.ErrorMessage = ex.Message;
                }

                _context.NotificationLogs.Add(log);
            }

            await _context.SaveChangesAsync();
        }

        public async Task SendPasswordResetAsync(
            ApplicationUser user,
            string resetUrl,
            string whatsAppButtonToken)
        {
            const string title = "Reset your Couples Connect password";
            const string message = "A password reset was requested for your Couples Connect account.";

            await CreateNotification(user.Id, NotificationType.PasswordReset, title, message, resetUrl);

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var email = await _templates.BuildPasswordResetAsync(user.FullName, resetUrl);
                await SendEmail(user, NotificationType.PasswordReset, email.Subject, message, email);
            }

            if (!string.IsNullOrWhiteSpace(user.Phone))
            {
                var log = CreateLog(
                    NotificationChannel.WhatsApp,
                    NotificationType.PasswordReset,
                    user.Id,
                    null,
                    user.Phone,
                    title,
                    message);

                try
                {
                    var sent = await _messaging.SendWhatsAppPasswordResetAsync(
                        user.Phone,
                        user.FullName,
                        whatsAppButtonToken);

                    log.Status = sent ? "Sent" : "Failed";
                    if (sent)
                        log.SentDate = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    log.Status = "Failed";
                    log.ErrorMessage = ex.Message;
                }

                _context.NotificationLogs.Add(log);
            }

            await _context.SaveChangesAsync();
        }

        public async Task SendPasswordChangedAlertAsync(ApplicationUser user)
        {
            const string title = "Your password was changed";
            const string message = "Your Couples Connect password was changed successfully. If this was not you, reset your password immediately.";

            await CreateNotification(user.Id, NotificationType.PasswordChanged, title, message);

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var email = await _templates.BuildPasswordChangedAlertAsync(user.FullName);
                await SendEmail(user, NotificationType.PasswordChanged, email.Subject, message, email);
            }

            if (!string.IsNullOrWhiteSpace(user.Phone))
            {
                var log = CreateLog(
                    NotificationChannel.WhatsApp,
                    NotificationType.PasswordChanged,
                    user.Id,
                    null,
                    user.Phone,
                    title,
                    message);

                try
                {
                    var sent = await _messaging.SendWhatsAppPasswordChangedAsync(user.Phone, user.FullName);
                    log.Status = sent ? "Sent" : "Failed";
                    if (sent)
                        log.SentDate = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    log.Status = "Failed";
                    log.ErrorMessage = ex.Message;
                }

                _context.NotificationLogs.Add(log);
            }

            await _context.SaveChangesAsync();
        }

        public async Task NotifyCounselorApplicationSubmittedAsync(Guid counselorUserId)
        {
            var profile = await _context.CounselorProfiles.Include(item => item.User)
                .FirstOrDefaultAsync(item => item.UserId == counselorUserId);
            if (profile is null) return;

            var reviewUrl = $"{await GetBaseUrl()}/Admin/Counselors";
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            const string title = "New counselor application";
            var message = $"{profile.FullName} submitted a counselor application for review.";

            foreach (var admin in admins)
            {
                await CreateNotification(admin.Id, NotificationType.CounselorApplicationSubmitted, title, message, reviewUrl);
                if (!string.IsNullOrWhiteSpace(admin.Email))
                {
                    var email = await _templates.BuildCounselorApplicationReceivedAsync(admin.FullName, profile.FullName, profile.User.Email ?? "Not provided", profile.YearsOfExperience, reviewUrl);
                    await SendEmail(admin, NotificationType.CounselorApplicationSubmitted, email.Subject, message, email);
                }
                await SendCounselorWhatsAppAsync(admin, NotificationType.CounselorApplicationSubmitted, title, message,
                    "counselor_application_received", profile.FullName, profile.User.Email ?? "Not provided", profile.YearsOfExperience.ToString());
            }

            await _context.SaveChangesAsync();
        }

        public async Task NotifyCounselorApplicationApprovedAsync(Guid counselorUserId)
        {
            var profile = await _context.CounselorProfiles.Include(item => item.User)
                .FirstOrDefaultAsync(item => item.UserId == counselorUserId);
            if (profile is null) return;

            const string title = "Counselor application approved";
            const string message = "Your counselor application has been approved. You can now access counselor tools in your account.";
            var dashboardUrl = $"{await GetBaseUrl()}/DashboardRedirect";
            await CreateNotification(profile.UserId, NotificationType.CounselorApplicationApproved, title, message, dashboardUrl);
            if (!string.IsNullOrWhiteSpace(profile.User.Email))
            {
                var email = await _templates.BuildCounselorApplicationApprovedAsync(profile.FullName, dashboardUrl);
                await SendEmail(profile.User, NotificationType.CounselorApplicationApproved, email.Subject, message, email);
            }
            await SendCounselorWhatsAppAsync(profile.User, NotificationType.CounselorApplicationApproved, title, message,
                "counselor_application_approved", profile.FullName);
            await _context.SaveChangesAsync();
        }

        public async Task NotifyCounselorApplicationRejectedAsync(Guid counselorUserId, string reason)
        {
            var profile = await _context.CounselorProfiles.Include(item => item.User)
                .FirstOrDefaultAsync(item => item.UserId == counselorUserId);
            if (profile is null) return;

            const string title = "Counselor application update";
            var message = $"Your counselor application was not approved. Reason: {reason}";
            await CreateNotification(profile.UserId, NotificationType.CounselorApplicationRejected, title, message);
            if (!string.IsNullOrWhiteSpace(profile.User.Email))
            {
                var email = await _templates.BuildCounselorApplicationRejectedAsync(profile.FullName, reason);
                await SendEmail(profile.User, NotificationType.CounselorApplicationRejected, email.Subject, message, email);
            }
            await SendCounselorWhatsAppAsync(profile.User, NotificationType.CounselorApplicationRejected, title, message,
                "counselor_application_rejected", profile.FullName, reason);
            await _context.SaveChangesAsync();
        }

        private async Task SendCounselorWhatsAppAsync(ApplicationUser user, NotificationType type, string title, string message, string templateName, params string[] parameters)
        {
            if (string.IsNullOrWhiteSpace(user.Phone)) return;
            var log = CreateLog(NotificationChannel.WhatsApp, type, user.Id, null, user.Phone, title, message);
            try
            {
                var sent = await _messaging.SendWhatsAppTemplateAsync(user.Phone, templateName, parameters);
                log.Status = sent ? "Sent" : "Failed";
                if (sent) log.SentDate = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                log.Status = "Failed";
                log.ErrorMessage = ex.Message;
            }
            _context.NotificationLogs.Add(log);
        }

        private Task CreateNotification(
    Guid userId,
    NotificationType type,
    string title,
    string message,
    string? link = null)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type.ToString(),
                Link = link,
                IsRead = false,
                CreatedDate = DateTime.UtcNow
            });

            return Task.CompletedTask;
        }

        private NotificationLog CreateLog(
    NotificationChannel channel,
    NotificationType type,
    Guid? userId,
    Guid? bookingId,
    string recipient,
    string subject,
    string message)
        {
            return new NotificationLog
            {
                Channel = channel.ToString(),
                NotificationType = type.ToString(),
                UserId = userId,
                BookingId = bookingId,
                Recipient = recipient,
                Subject = subject,
                Message = message,
                CreatedDate = DateTime.UtcNow,
                Status = "Pending"
            };
        }



        private async Task NotifyUserAsync(
    ApplicationUser user,
    Guid? bookingId,
    NotificationType type,
    string title,
    string message,
    EmailMessage? email = null,
    string? link = null,
    bool sendWhatsApp = false)
        {
            await CreateNotification(
                user.Id,
                type,
                title,
                message,
                link);

            if (email != null && !string.IsNullOrWhiteSpace(user.Email))
            {
                var sent = await _messaging.SendEmailAsync(
                    user.Email,
                    email);

                var log = CreateLog(
                    NotificationChannel.Email,
                    type,
                    user.Id,
                    bookingId,
                    user.Email,
                    email.Subject,
                    message);

                log.Status = sent ? "Sent" : "Failed";

                if (sent)
                    log.SentDate = DateTime.UtcNow;

                _context.NotificationLogs.Add(log);
            }

            if (sendWhatsApp &&
                !string.IsNullOrWhiteSpace(user.Phone))
            {
                var sent = await _messaging.SendWhatsAppAsync(
                   user.Phone,"general_notification",
                    message);

                var log = CreateLog(
                    NotificationChannel.WhatsApp,
                    type,
                    user.Id,
                    bookingId,
                    user.Phone,
                    title,
                    message);

               log.Status = sent ? "Sent" : "Failed";

               if (sent)
                 log.SentDate = DateTime.Now;

                _context.NotificationLogs.Add(log);
            }
        }
        private async Task SendEmail(
    ApplicationUser user,
    NotificationType type,
    string subject,
    string message,
    EmailMessage? email = null)
        {
            var log = CreateLog(
                NotificationChannel.Email,
                type,
                user.Id,
                null,
                user.Email ?? "",
                subject,
                message);

            try
            {
                email ??= new EmailMessage
                {
                    Subject = subject,
                    HtmlBody = await _templates.BuildTemplateAsync(subject, message)
                };

                var sent = await _messaging.SendEmailAsync(
                    user.Email!,
                    email);

                log.Status = sent ? "Sent" : "Failed";

                if (sent)
                    log.SentDate = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                log.Status = "Failed";
                log.ErrorMessage = ex.Message;
            }

            _context.NotificationLogs.Add(log);
        }


        private async Task SendWhatsApp(
    ApplicationUser user,
    NotificationType type,
    string message)
        {
            var log = CreateLog(
                NotificationChannel.WhatsApp,
                type,
                user.Id,
                null,
                user.PhoneNumber ?? "",
                "",
                message);

            try
            {
               var sent = await _messaging.SendWhatsAppAsync(
                   user.PhoneNumber!, "general_notification",
                   message);

               log.Status = sent ? "Sent" : "Failed";

               if (sent)
                   log.SentDate = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                log.Status = "Failed";
                log.ErrorMessage = ex.Message;
            }

            _context.NotificationLogs.Add(log);
        }





        public async Task NotifyBookingCreatedAsync(Guid bookingId)
        {
            var booking = await _context.Bookings
                .Include(x => x.User)
                .Include(x => x.Counselor)
                .Include(x => x.Service)
                .Include(x => x.Session)
                .FirstOrDefaultAsync(x => x.BookingId == bookingId);

            if (booking == null)
                return;

            var settings = await _settings.GetAsync();

            var bookingUrl =
    $"{await GetBaseUrl()}/User/Bookings/Index";

            var title = "Booking Confirmed";

            var message =
$@"Your counselling session has been confirmed.

{BuildBookingSummary(booking)}";

            var email =
                await _templates.BuildBookingConfirmationAsync(
                    booking.User.FullName,
                    booking.Counselor.FullName,
                    booking.ScheduledStart,
                    bookingUrl);

            await NotifyUserAsync(
                booking.User,
                booking.BookingId,
                NotificationType.BookingCreated,
                title,
                message,
                email,
                bookingUrl, false);

            await _messaging.SendWhatsAppToClientAsync(booking.User.Phone, "notify_client",
                booking.User.FullName, booking.Counselor.FullName, booking.Service.Name, $"{booking.ScheduledStart:h: mm tt}",
                $"{booking.ScheduledStart:dddd, dd MMM yyyy}");

            await _context.SaveChangesAsync();
        }

        public async Task NotifyCounselorBookingCreatedAsync(Guid bookingId)
        {
            var booking = await _context.Bookings
                .Include(x => x.User)
                .Include(x => x.Counselor)
                .Include(x => x.Service)
                .Include(x => x.Session)
                .FirstOrDefaultAsync(x => x.BookingId == bookingId);

            if (booking == null)
                return;

            var settings = await _settings.GetAsync();

            var bookingUrl =
    $"{await GetBaseUrl()}/Counselor/Sessions/Index";

            var title = "Booking Confirmed";

            var message =
$@"You have a new booking.

{BuildCounsellorBookingSummary(booking)}";

            var email =
                await _templates.BuildBookingConfirmationAsync(
                    booking.User.FullName,
                    booking.Counselor.FullName,
                    booking.ScheduledStart,
                    bookingUrl);

            await NotifyUserAsync(
                booking.Counselor,
                booking.BookingId,
                NotificationType.BookingCreated,
                title,
                message,
                email,
                bookingUrl, false);

            await _messaging.SendWhatsAppToCounselorAsync(booking.Counselor.Phone, "notify_counselor",
                booking.Counselor.FullName, booking.User.FullName, booking.Service.Name, $"{booking.ScheduledStart:h: mm tt}",
                $"{booking.ScheduledStart:dddd, dd MMM yyyy}");

            await _context.SaveChangesAsync();
        }
        public async Task NotifyBookingReminderAsync(Guid bookingId)
        {
            var booking = await _context.Bookings
                .Include(x => x.User)
                .Include(x => x.Counselor)
                .Include(x => x.Service)
                .Include(x => x.Session)
                .Include(x => x.Participants)
                    .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(x => x.BookingId == bookingId);

            if (booking == null)
                return;

            var joinUrl =
    $"{await GetBaseUrl()}/Sessions/PreJoin/{booking.Session.SessionId}";

            var title = "Session Reminder";

            var message =
$@"Your counselling session starts soon.

{BuildBookingSummary(booking)}

Please join a few minutes before the scheduled time.";

            var email =
                await _templates.BuildBookingReminderAsync(
                    booking.User.FullName,
                    booking.Counselor.FullName,
                    booking.ScheduledStart,
                    joinUrl);

            await NotifyUserAsync(
                booking.User,
                booking.BookingId,
                NotificationType.BookingReminder,
                title,
                message,
                email,
                joinUrl);

            await SendBookingReminderWhatsAppAsync(booking.User, booking, isCounselor: false);

            foreach (var participant in booking.Participants.Where(item => item.Status == "Accepted" && item.User != null))
            {
                var participantEmail = await _templates.BuildBookingReminderAsync(
                    participant.User!.FullName,
                    booking.Counselor.FullName,
                    booking.ScheduledStart,
                    joinUrl);

                await NotifyUserAsync(
                    participant.User,
                    booking.BookingId,
                    NotificationType.BookingReminder,
                    title,
                    message,
                    participantEmail,
                    joinUrl);

                await SendBookingReminderWhatsAppAsync(participant.User, booking, isCounselor: false);
            }

            var counselorMessage = $"Your counselling session with {booking.User.FullName} starts soon.\n\n{BuildCounsellorBookingSummary(booking)}";
            var counselorEmail = new EmailMessage
            {
                Subject = "Session Reminder",
                HtmlBody = await _templates.BuildTemplateAsync(
                    "Session Reminder",
                    $"<p>Hello <strong>{booking.Counselor.FullName}</strong>,</p><p>Your session with <strong>{booking.User.FullName}</strong> starts soon.</p>",
                    "Open session",
                    joinUrl)
            };

            await NotifyUserAsync(
                booking.Counselor,
                booking.BookingId,
                NotificationType.BookingReminder,
                title,
                counselorMessage,
                counselorEmail,
                joinUrl);

            await SendBookingReminderWhatsAppAsync(booking.Counselor, booking, isCounselor: true);

            await _context.SaveChangesAsync();
        }

        private async Task SendBookingReminderWhatsAppAsync(ApplicationUser recipient, Booking booking, bool isCounselor)
        {
            if (string.IsNullOrWhiteSpace(recipient.Phone))
                return;

            const string title = "Session Reminder";
            var message = isCounselor
                ? $"Your counselling session with {booking.User.FullName} starts soon."
                : $"Your counselling session with {booking.Counselor.FullName} starts soon.";
            var log = CreateLog(NotificationChannel.WhatsApp, NotificationType.BookingReminder, recipient.Id,
                booking.BookingId, recipient.Phone, title, message);

            try
            {
                var sent = isCounselor
                    ? await _messaging.SendWhatsAppToCounselorAsync(recipient.Phone, "session_reminder_counselor", recipient.FullName, booking.User.FullName, booking.Service.Name, $"{booking.ScheduledStart:h:mm tt}", $"{booking.ScheduledStart:dddd, dd MMM yyyy}")
                    : await _messaging.SendWhatsAppToClientAsync(recipient.Phone, "session_reminder_client", recipient.FullName, booking.Counselor.FullName, booking.Service.Name, $"{booking.ScheduledStart:h:mm tt}", $"{booking.ScheduledStart:dddd, dd MMM yyyy}");

                log.Status = sent ? "Sent" : "Failed";
                if (sent)
                    log.SentDate = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                log.Status = "Failed";
                log.ErrorMessage = ex.Message;
            }

            _context.NotificationLogs.Add(log);
        }

        public async Task NotifyInvitationAsync(int participantId)
        {
            var participant = await _context.BookingParticipants
                .Include(x => x.Booking)
                    .ThenInclude(x => x.User)
                .Include(x => x.Booking)
                    .ThenInclude(x => x.Counselor)
                .FirstOrDefaultAsync(x => x.Id == participantId);

            if (participant == null)
                return;

            var baseUrl = await GetBaseUrl();

            var acceptUrl =
                $"{baseUrl}/User/Bookings/Accept?token={participant.InviteToken}";

            var title = "Counselling Invitation";

            var message =
        $@"You have been invited by {participant.Booking.User.FullName} to join a counselling session.

        Click the invitation link to accept.";

            if (participant.User != null)
            {
                var email =
                    await _templates.BuildInvitationAsync(
                        participant.Booking.User.FullName,
                        acceptUrl);

                await NotifyUserAsync(
                    participant.User,
                    participant.Booking.BookingId,
                    NotificationType.Invitation,
                    title,
                    message,
                    email,
                    acceptUrl);
            }
            else
            {
                var sent = await _messaging.SendWhatsAppWithButtonAsync(participant.PhoneNumber, participant.Booking.User.FullName,
                    "Mate", participant.InviteToken, "group_session_invitation", participant.Booking.ScheduledStart.ToString());

        var log = CreateLog(
            NotificationChannel.WhatsApp,
            NotificationType.Invitation,
            null,
            participant.Booking.BookingId,
            participant.PhoneNumber,
            title,
            message);

                log.Status = sent
                    ? "Sent"
                    : "Failed";

                if (sent)
                    log.SentDate = DateTime.UtcNow;

                _context.NotificationLogs.Add(log);
            }

            await _context.SaveChangesAsync();
        }

        public async Task NotifyInvitationAcceptedAsync(int participantId)
        {
            var participant = await _context.BookingParticipants
                .Include(x => x.Booking)
                    .ThenInclude(x => x.User)
                .Include(x => x.Booking)
                    .ThenInclude(x => x.Counselor)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == participantId);

            if (participant == null)
                return;

            var bookingOwner = participant.Booking.User;

            var participantName =
                participant.User?.FullName ??
                participant.PhoneNumber;

            var bookingUrl =
                $"{await GetBaseUrl()}/User/Bookings/Index";

            var title = "Invitation Accepted";

            var message =
        $@"{participantName}

has accepted your counselling invitation.

Your session is now ready.";

            var email =
    await _templates.BuildInvitationAcceptedAsync(
        participantName);

            await NotifyUserAsync(
    bookingOwner,
    participant.Booking.BookingId,
    NotificationType.InvitationAccepted,
    title,
    message,
    email,
    bookingUrl);

            await _context.SaveChangesAsync();
        }



        public async Task NotifySessionStartedAsync(Guid bookingId)
        {
            var booking = await _context.Bookings
                .Include(x => x.User)
                .Include(x => x.Counselor)
                .Include(x => x.Service)
                .Include(x => x.Session)
                .Include(x => x.Participants)
                    .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(x => x.BookingId == bookingId);

            if (booking == null)
                return;

            var joinUrl =
                $"{await GetBaseUrl()}/Sessions/PreJoin/{booking.Session.SessionId}";

            var title = "Your Session Has Started";

            var summary = BuildBookingSummary(booking);

            var message =
        $@"Your counselling session has started.

{summary}

Click below to join now.";

            var ownerEmail =
    await _templates.BuildSessionStartedAsync(
        booking.User.FullName,
        joinUrl);

            await NotifyUserAsync(
                booking.User,
                booking.BookingId,
                NotificationType.SessionStarted,
                title,
                message,
                ownerEmail,
                joinUrl);

            var acceptedParticipants = await _context.BookingParticipants
    .Include(p => p.User)
    .Where(p =>
        p.BookingId == bookingId &&
        p.Status == "Accepted" &&
        p.UserId != null)
    .ToListAsync();

            foreach (var participant in acceptedParticipants)
            {
                var email =
                    await _templates.BuildSessionStartedAsync(
                        participant.User.FullName,
                        joinUrl);

                await NotifyUserAsync(
                    participant.User,
                    booking.BookingId,
                    NotificationType.SessionStarted,
                    title,
                    message,
                    email,
                    joinUrl);


            }


            var notifyCounsellor =
    (await _settings.GetAsync()).NotifyCounsellorOnSessionStart;

            if (notifyCounsellor)
            {
                var email =
                    await _templates.BuildSessionStartedAsync(
                        booking.Counselor.FullName,
                        joinUrl);

                await NotifyUserAsync(
                    booking.Counselor,
                    booking.BookingId,
                    NotificationType.SessionStarted,
                    title,
                    message,
                    email,
                    joinUrl);
            }

            await _context.SaveChangesAsync();
        }


        public async Task NotifySessionCompletedAsync(Guid bookingId)
        {
            var booking = await _context.Bookings
                .Include(x => x.User)
                .Include(x => x.Counselor)
                .Include(x => x.Service)
                .Include(x => x.Session)
                .FirstOrDefaultAsync(x => x.BookingId == bookingId);

            if (booking == null)
                return;

            var baseUrl = await GetBaseUrl();

            var feedbackUrl =
                $"{baseUrl}/User/Bookings/Feedback/{booking.BookingId}";

            var bookingUrl =
                $"{baseUrl}/User/Bookings/Index";

            var title = "Session Completed";

            var summary = BuildBookingSummary(booking);

            var message =
        $@"Thank you for attending your counselling session.

{summary}

We hope you found the session helpful.

We'd appreciate your feedback.";

            var ownerEmail =
    await _templates.BuildSessionCompletedAsync(
        booking.User.FullName);

            await NotifyUserAsync(
                booking.User,
                booking.BookingId,
                NotificationType.SessionCompleted,
                title,
                message,
                ownerEmail,
                feedbackUrl);
            var participants = await _context.BookingParticipants
    .Include(x => x.User)
    .Where(x =>
        x.BookingId == booking.BookingId &&
        x.Status == "Accepted" &&
        x.UserId != null)
    .ToListAsync();

            foreach (var participant in participants)
            {
                var email =
                    await _templates.BuildSessionCompletedAsync(
                        participant.User.FullName);

                await NotifyUserAsync(
                    participant.User,
                    booking.BookingId,
                    NotificationType.SessionCompleted,
                    title,
                    message,
                    email,
                    feedbackUrl);
            }

            var counsellorEmail =
    await _templates.BuildSessionCompletedAsync(
        booking.Counselor.FullName);

            await NotifyUserAsync(
                booking.Counselor,
                booking.BookingId,
                NotificationType.SessionCompleted,
                "Session Finished",
                $"The counselling session with {booking.User.FullName} has ended.",
                counsellorEmail,
                bookingUrl,
                sendWhatsApp: false);

            await _context.SaveChangesAsync();
        }
        private async Task<string> GetBaseUrl()
        {
            var settings = await _settings.GetAsync();

            return settings.BaseUrl.TrimEnd('/');
        }

        private string BuildBookingSummary(Booking booking)
        {
            return
        $@"Counsellor: {booking.Counselor.FullName}

Service: {booking.Service.Name}

Date: {booking.ScheduledStart:dddd, dd MMM yyyy}

Time: {booking.ScheduledStart:h:mm tt}";
        }


        private string BuildCounsellorBookingSummary(Booking booking)
        {
            return
        $@"Client: {booking.User.FullName}

Service: {booking.Service.Name}

Date: {booking.ScheduledStart:dddd, dd MMM yyyy}

Time: {booking.ScheduledStart:h:mm tt}";
        }
    }
}
