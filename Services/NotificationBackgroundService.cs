using CouplesConnect.Data;
using CouplesConnect.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class NotificationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public NotificationBackgroundService(
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {

        try
        {


            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessNotifications();

                await Task.Delay(
                    TimeSpan.FromMinutes(1),
                    stoppingToken);
            }
        }

        catch (Exception ex)

        {
            Console.WriteLine(ex.ToString());
        }

    }

    private async Task ProcessNotifications()
    {
        using var scope =
            _scopeFactory.CreateScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var notifications =
            scope.ServiceProvider
                .GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;

        var reminders = await db.Bookings
    .Where(x =>
        !x.ReminderSent &&
        x.BookingStatus == "Scheduled" &&
        x.ScheduledStart <= now.AddMinutes(10) &&
        x.ScheduledStart > now)
    .Select(x => x.BookingId)
    .ToListAsync();

        foreach (var bookingId in reminders)
        {
            await notifications.NotifyBookingReminderAsync(
                bookingId);

            var booking = await db.Bookings.FindAsync(bookingId);

            booking.ReminderSent = true;
            booking.ReminderSentDate = now;
        }

        await db.SaveChangesAsync();

        var started = await db.Bookings
    .Where(x =>
        !x.SessionStartedNotificationSent &&
        x.BookingStatus == "Scheduled" &&
        x.ScheduledStart <= now)
    .Select(x => x.BookingId)
    .ToListAsync();

        foreach (var bookingId in started)
        {
            await notifications.NotifySessionStartedAsync(
                bookingId);

            var booking = await db.Bookings.FindAsync(bookingId);

            booking.SessionStartedNotificationSent = true;
            booking.SessionStartedNotificationDate = now;
        }

        await db.SaveChangesAsync();

        var completed = await db.Bookings
    .Where(x =>
        !x.SessionCompletedNotificationSent &&
        x.BookingStatus == "Scheduled")
    .Select(x => x.BookingId)
    .ToListAsync();

        foreach (var bookingId in completed)
        {
            await notifications.NotifySessionCompletedAsync(
                bookingId);

            var booking = await db.Bookings.FindAsync(bookingId);

            booking.SessionCompletedNotificationSent = true;
            booking.SessionCompletedNotificationDate = now;
        }

        await db.SaveChangesAsync();

    }
    }


