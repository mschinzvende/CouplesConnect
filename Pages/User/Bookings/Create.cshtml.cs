using CouplesConnect.Data;
using CouplesConnect.Data.Models;
using CouplesConnect.Models;
using CouplesConnect.Services;
using CouplesConnect.Services.Interfaces;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.User.Bookings
{
    [Authorize(Roles = "User")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notification;

        public CreateModel(ApplicationDbContext context,
                           UserManager<ApplicationUser> userManager,
                           INotificationService notification)
        {
            _context = context;
            _userManager = userManager;
            _notification = notification;
        }

        public List<Service> Services { get; set; }
        public List<ApplicationUser> Counselors { get; set; }

        public int? ServiceId { get; set; }
        public Guid CounselorId { get; set; }
        public DateTime? SelectedDate { get; set; }

        public List<DateTime> AvailableDates { get; set; } = new();
        public List<DateTime> TimeSlots { get; set; } = new();

        public async Task OnGetAsync(int? ServiceId, Guid CounselorId, DateTime? SelectedDate)
        {
            this.ServiceId = ServiceId;
            this.CounselorId = CounselorId;
            this.SelectedDate = SelectedDate;

            Services = await _context.Services
                .Where(x => x.IsActive)
                .ToListAsync();

            var counselorProfiles = _context.CounselorProfiles
                .Where(profile => profile.IsApproved)
                .Include(profile => profile.User)
                .AsQueryable();

            if (ServiceId.HasValue)
            {
                counselorProfiles = counselorProfiles.Where(profile =>
                    _context.CounselorServices.Any(assignment =>
                        assignment.CounselorId == profile.UserId && assignment.ServiceId == ServiceId.Value));
            }

            Counselors = await counselorProfiles
                .Select(profile => profile.User)
                .ToListAsync();

            if (CounselorId == null)
                return;

            var now = DateTime.Now;

            // ? GET AVAILABILITY (FULL DATETIME)
            var availability = await _context.CounselorAvailabilities
                .Where(x => x.CounselorId == CounselorId)
                .ToListAsync();

            // =========================
            // AVAILABLE DATES (NEXT 7 DAYS)
            // =========================
            for (int i = 0; i < 7; i++)
            {
                var date = DateTime.Today.AddDays(i);

                // slots for this specific date
                var daySlots = availability
                    .Where(a => a.StartDateTime.Date == date.Date)
                    .ToList();

                if (!daySlots.Any())
                    continue;

                var potentialSlots = new List<DateTime>();

                foreach (var slot in daySlots)
                {
                    var start = slot.StartDateTime;
                    var end = slot.EndDateTime;

                    while (start < end)
                    {
                        if (start > now)
                            potentialSlots.Add(start);

                        start = start.AddHours(1);
                    }
                }

                // booked slots
                var booked = await _context.Bookings
                    .Where(b => b.CounselorId == CounselorId &&
                                b.ScheduledStart.Date == date.Date &&
                                b.BookingStatus != "Cancelled")
                    .Select(b => b.ScheduledStart)
                    .ToListAsync();

                var availableSlots = potentialSlots
                    .Where(t => !booked.Contains(t))
                    .ToList();

                if (availableSlots.Any())
                {
                    AvailableDates.Add(date);
                }
            }

            // =========================
            // TIMESLOTS FOR SELECTED DATE
            // =========================
            if (SelectedDate.HasValue)
            {
                var date = SelectedDate.Value.Date;

                var daySlots = availability
                    .Where(a => a.StartDateTime.Date == date)
                    .ToList();

                foreach (var slot in daySlots)
                {
                    var start = slot.StartDateTime;
                    var end = slot.EndDateTime;

                    while (start < end)
                    {
                        if (start > now)
                        {
                            TimeSlots.Add(start);
                        }

                        start = start.AddHours(1);
                    }
                }

                // remove booked
                var booked = await _context.Bookings
                    .Where(b => b.CounselorId == CounselorId &&
                                b.ScheduledStart.Date == date &&
                                b.BookingStatus != "Cancelled")
                    .Select(b => b.ScheduledStart)
                    .ToListAsync();

                TimeSlots = TimeSlots
                    .Where(t => !booked.Contains(t))
                    .OrderBy(t => t)
                    .ToList();
            }
        }

        public async Task<IActionResult> OnPostAsync(
    int ServiceId,
    Guid CounselorId,
    DateTime ScheduledStart,
    bool isGroup,
    List<string> ParticipantPhones)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
                return Challenge();

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var now = DateTime.UtcNow;
            var sub = await _context.UserSubscriptions.FirstOrDefaultAsync(x =>
                x.UserId == user.Id && x.IsActive && x.SessionsRemaining > 0 &&
                (!x.EndDate.HasValue || x.EndDate > now));
            if (sub is null)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "No active session credits remain. Please purchase a package.";
                return Redirect("/User/Packages");
            }

            var session = new Session
            {
                MeetingLink = "https://meet.jitsi/" + Guid.NewGuid()
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            var booking = new Booking
            {
                UserId = user.Id,
                CounselorId = CounselorId,
                ServiceId = ServiceId,
                ScheduledStart = ScheduledStart,
                SessionId = session.SessionId,
                SubscriptionId = sub.SubscriptionId,
                IsGroup = isGroup
            };

            _context.Bookings.Add(booking);
            sub.SessionsRemaining--;
            await _context.SaveChangesAsync();

            // GROUP PARTICIPANTS
            var participants = new List<BookingParticipant>();
            if (isGroup && ParticipantPhones != null)
            {
                foreach (var phone in ParticipantPhones.Where(x => !string.IsNullOrEmpty(x)))
                {
                    var token = Guid.NewGuid().ToString();

                    var participant = new BookingParticipant
                    {
                        BookingId = booking.BookingId,
                        PhoneNumber = phone,
                        InviteToken = token
                    };

                    _context.BookingParticipants.Add(participant);
                    participants.Add(participant);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _notification.NotifyBookingCreatedAsync(booking.BookingId);
            await _notification.NotifyCounselorBookingCreatedAsync(booking.BookingId);
            foreach (var participant in participants)
                await _notification.NotifyInvitationAsync(participant.Id);

            TempData["Success"] = "Session booked successfully";
            return Redirect("/User/Bookings");
        }
    }
    }
