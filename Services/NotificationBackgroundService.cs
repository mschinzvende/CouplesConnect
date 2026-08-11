using CouplesConnect.Data;
using CouplesConnect.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class NotificationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationBackgroundService> _logger;

    public NotificationBackgroundService(IServiceScopeFactory scopeFactory, ILogger<NotificationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNotificationsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing scheduled notifications.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessNotificationsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var now = DateTime.UtcNow;

        // Repair bookings marked as completed by the previous faulty scheduled-booking query.
        // A scheduled booking has not completed and must remain eligible for its real completion notice.
        var incorrectlyMarkedScheduledBookings = await db.Bookings
            .Where(booking => booking.BookingStatus == "Scheduled" && booking.SessionCompletedNotificationSent)
            .ToListAsync(stoppingToken);

        foreach (var booking in incorrectlyMarkedScheduledBookings)
        {
            booking.SessionCompletedNotificationSent = false;
            booking.SessionCompletedNotificationDate = null;
        }

        if (incorrectlyMarkedScheduledBookings.Count > 0)
            await db.SaveChangesAsync(stoppingToken);

        var reminders = await db.Bookings
            .Where(booking => !booking.ReminderSent && booking.BookingStatus == "Scheduled" &&
                booking.ScheduledStart <= now.AddMinutes(10) && booking.ScheduledStart > now)
            .Select(booking => booking.BookingId)
            .ToListAsync(stoppingToken);

        foreach (var bookingId in reminders)
        {
            stoppingToken.ThrowIfCancellationRequested();
            await notifications.NotifyBookingReminderAsync(bookingId);
            var booking = await db.Bookings.FindAsync(new object[] { bookingId }, stoppingToken);
            if (booking != null)
            {
                booking.ReminderSent = true;
                booking.ReminderSentDate = now;
            }
        }
        await db.SaveChangesAsync(stoppingToken);

        var started = await db.Bookings
            .Where(booking => !booking.SessionStartedNotificationSent && booking.BookingStatus == "Scheduled" && booking.ScheduledStart <= now)
            .Select(booking => booking.BookingId)
            .ToListAsync(stoppingToken);

        foreach (var bookingId in started)
        {
            stoppingToken.ThrowIfCancellationRequested();
            await notifications.NotifySessionStartedAsync(bookingId);
            var booking = await db.Bookings.FindAsync(new object[] { bookingId }, stoppingToken);
            if (booking != null)
            {
                booking.SessionStartedNotificationSent = true;
                booking.SessionStartedNotificationDate = now;
            }
        }
        await db.SaveChangesAsync(stoppingToken);

        var completed = await db.Bookings
            .Where(booking => !booking.SessionCompletedNotificationSent &&
                booking.BookingStatus == "Completed" && booking.Session.IsEnded)
            .Select(booking => booking.BookingId)
            .ToListAsync(stoppingToken);

        foreach (var bookingId in completed)
        {
            stoppingToken.ThrowIfCancellationRequested();
            await notifications.NotifySessionCompletedAsync(bookingId);
            var booking = await db.Bookings.FindAsync(new object[] { bookingId }, stoppingToken);
            if (booking != null)
            {
                booking.SessionCompletedNotificationSent = true;
                booking.SessionCompletedNotificationDate = now;
            }
        }
        await db.SaveChangesAsync(stoppingToken);
    }
}
