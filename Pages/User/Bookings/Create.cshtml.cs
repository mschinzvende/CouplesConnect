using CouplesConnect.Data;
using CouplesConnect.Data.Models;
using CouplesConnect.Models;
using CouplesConnect.Services;
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
        private readonly SubscriptionService _subscriptionService;

        public CreateModel(ApplicationDbContext context,
                           UserManager<ApplicationUser> userManager,
                           SubscriptionService subscriptionService)
        {
            _context = context;
            _userManager = userManager;
            _subscriptionService = subscriptionService;
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

            Services = await _context.Services.Where(x => x.IsActive).ToListAsync();

            Counselors = await _context.CounselorProfiles
    .Where(x => x.IsApproved)
    .Include(x => x.User)
    .Select(x => x.User)
    .ToListAsync();

            if (CounselorId != null)
            {
                var availability = await _context.CounselorAvailabilities
                    .Where(x => x.CounselorId == CounselorId)
                    .ToListAsync();

                // Next 7 days
                var now = DateTime.Now;

                // Next 7 days
                for (int i = 0; i < 7; i++)
                {
                    var date = DateTime.Today.AddDays(i);

                    var daySlots = availability
                        .Where(a => a.DayOfWeek == date.DayOfWeek)
                        .ToList();

                    if (!daySlots.Any())
                        continue;

                    var potentialSlots = new List<DateTime>();

                    foreach (var slot in daySlots)
                    {
                        var start = date.Date.Add(slot.StartTime);
                        var end = date.Date.Add(slot.EndTime);

                        while (start < end)
                        {
                            // only future slots
                            if (start > now)
                            {
                                potentialSlots.Add(start);
                            }

                            start = start.AddMinutes(60);
                        }
                    }

                    // remove already booked
                    var booked = await _context.Bookings
                        .Where(b => b.CounselorId == CounselorId &&
                                    b.ScheduledStart.Date == date.Date && b.BookingStatus != "Cancelled")
                        .Select(b => b.ScheduledStart)
                        .ToListAsync();

                    var availableSlots = potentialSlots
                        .Where(t => !booked.Contains(t))
                        .ToList();

                    // ? ONLY add date if it still has free slots
                    if (availableSlots.Any())
                    {
                        AvailableDates.Add(date);
                    }
                }

                if (SelectedDate.HasValue)
                {
                    var daySlots = availability
                        .Where(a => a.DayOfWeek == SelectedDate.Value.DayOfWeek);

                    foreach (var slot in daySlots)
                    {
                        var start = SelectedDate.Value.Date.Add(slot.StartTime);
                        var end = SelectedDate.Value.Date.Add(slot.EndTime);



                        while (start < end)
                        {
                            if (start > now) // only future slots
                            {
                                TimeSlots.Add(start);
                            }

                            start = start.AddMinutes(60); // 1-hour slots
                        }
                    }

                    // Remove already booked slots
                    var booked = await _context.Bookings
                        .Where(b => b.CounselorId == CounselorId &&
                                    b.ScheduledStart.Date == SelectedDate.Value.Date && b.BookingStatus != "Cancelled")
                        .Select(b => b.ScheduledStart)
                        .ToListAsync();

                    TimeSlots = TimeSlots
                        .Where(t => !booked.Contains(t))
                        .ToList();
                }
            }
        }

        public async Task<IActionResult> OnPostAsync(
    int ServiceId,
    Guid CounselorId,
    DateTime ScheduledStart,
    bool IsGroup,
    List<string> ParticipantPhones)
        {
            var user = await _userManager.GetUserAsync(User);

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
                IsGroup = IsGroup
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // GROUP PARTICIPANTS
            if (IsGroup && ParticipantPhones != null)
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

                    var link = $"{Request.Scheme}://{Request.Host}/User/Bookings/Accept?token={token}";

                    // TODO: integrate WhatsApp send here
                }
            }

            await _context.SaveChangesAsync();

            return Redirect("/User/Bookings");
        }
    }
    }