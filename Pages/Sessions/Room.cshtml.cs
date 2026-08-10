using CouplesConnect.Data;
using CouplesConnect.Data.Models;
using CouplesConnect.Models;
using CouplesConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CouplesConnect.Pages.Sessions
{
    [Authorize]
    public class RoomModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly AzureCommunicationTurnService _turnService;


        public RoomModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            AzureCommunicationTurnService turnService)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _turnService = turnService;
        }

        public int SessionId { get; set; }
        public bool IsCounselor { get; set; }
        public string ExistingNotes { get; set; }

        public List<Booking> UpcomingSessions { get; set; } = new();
        public DateTime? NextSessionTime { get; set; }

        public DateTime SessionStartTime { get; set; }
        public string IceServersJson { get; private set; } = "[]";
        public bool HasTurnServer { get; private set; }
        public string TurnStatus { get; private set; } = "TURN configuration has not been loaded.";

        public async Task<IActionResult> OnGetAsync(int sessionId)
        {
            SessionId = sessionId;
            await ConfigureIceServersAsync();
            

            var user = await _userManager.GetUserAsync(User);

            // ?? Get booking tied to session
            var booking = await _context.Bookings
                .Include(b => b.Session)
                .FirstOrDefaultAsync(b => b.Session.SessionId == sessionId);

            SessionStartTime = booking.ScheduledStart;

            if (booking == null)
                return NotFound();

            var isOwner = booking.UserId == user.Id;
            var isCounselor = booking.CounselorId == user.Id;

            /* ?? Access control
            if (!isOwner && !isCounselor)
                return Forbid();*/

            IsCounselor = isCounselor;

            // ?? Apply 5-min rule ONLY to users
            if (!isCounselor)
            {
                var minutesToStart = (booking.ScheduledStart - DateTime.Now).TotalMinutes;

                /*Temporary Deactivate constraint
                if (minutesToStart > 5)
                {
                    TempData["Error"] = "Session can only be joined 5 minutes before start time.";
                    return Redirect("/User/Dashboard");
                }*/
            }

            // ? Counselor logic (unchanged)
            if (IsCounselor)
            {
                var note = await _context.SessionNotes
                    .FirstOrDefaultAsync(x => x.SessionId == sessionId);

                ExistingNotes = note?.Notes;

                UpcomingSessions = await _context.Bookings
                    .Include(x => x.User)
                    .Include(x => x.Session)
                    .Where(x => x.CounselorId == user.Id &&
                                x.BookingStatus == "Scheduled" &&
                                x.ScheduledStart > DateTime.UtcNow)
                    .OrderBy(x => x.ScheduledStart)
                    .Take(5)
                    .ToListAsync();

                NextSessionTime = UpcomingSessions.FirstOrDefault()?.ScheduledStart;
            }

            return Page();
        }

        private async Task ConfigureIceServersAsync()
        {
            var relayConfiguration = await _turnService.GetIceServersAsync();
            var iceServers = relayConfiguration.IceServers.ToList();

            TurnStatus = relayConfiguration.Status;

            HasTurnServer = iceServers.Any(server =>
                server.Urls.Any(url => url.StartsWith("turn:", StringComparison.OrdinalIgnoreCase) ||
                                       url.StartsWith("turns:", StringComparison.OrdinalIgnoreCase)));

            if (iceServers.Count == 0)
            {
                var stunUrls = GetConfiguredUrls("WebRtc:StunUrls");

                if (stunUrls.Count > 0)
                {
                    iceServers.Add(new WebRtcIceServer { Urls = stunUrls });
                }

                var turnUrls = GetConfiguredUrls("WebRtc:Turn:Urls");
                var username = _configuration["WebRtc:Turn:Username"];
                var credential = _configuration["WebRtc:Turn:Credential"];

                if (turnUrls.Count > 0 &&
                    !string.IsNullOrWhiteSpace(username) &&
                    !string.IsNullOrWhiteSpace(credential))
                {
                    iceServers.Add(new WebRtcIceServer
                    {
                        Urls = turnUrls,
                        Username = username,
                        Credential = credential
                    });

                    HasTurnServer = true;
                }
            }

            IceServersJson = JsonSerializer.Serialize(
                iceServers,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
        }

        private List<string> GetConfiguredUrls(string sectionName) =>
            _configuration
                .GetSection(sectionName)
                .GetChildren()
                .Select(item => item.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .ToList();

        public async Task<IActionResult> OnPostSaveNotesAsync(SaveNotesDto dto)
        {
            try
            {
                if (dto == null || dto.SessionId == 0)
                {
                    return new JsonResult(new { success = false });
                }

                var user = await _userManager.GetUserAsync(User);
                var userId = user.Id;

                var existing = await _context.SessionNotes
                    .FirstOrDefaultAsync(x => x.SessionId == dto.SessionId);

                if (existing == null)
                {
                    _context.SessionNotes.Add(new SessionNote
                    {
                        SessionId = dto.SessionId,
                        CounselorId = userId,
                        Notes = dto.Notes ?? ""
                    });
                }
                else
                {
                    existing.Notes = dto.Notes ?? "";
                }

                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                // log this if needed
                return new JsonResult(new { success = false });
            }
        }

        public class SaveNotesDto
        {
            public int SessionId { get; set; }
            public string Notes { get; set; }
        }
    }
}
