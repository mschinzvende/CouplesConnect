using Microsoft.AspNetCore.SignalR;

using CouplesConnect.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CouplesConnect.Hubs
{
    public class SessionHub : Hub
    {
        private readonly ILogger<SessionHub> _logger;
        private readonly ApplicationDbContext _context;

        public SessionHub(ILogger<SessionHub> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        // ==========================================
        // Active Participants
        // ==========================================

        private static readonly Dictionary<int, HashSet<string>>
            ActiveSessions = new();

        // ==========================================
        // Waiting Room
        // ==========================================

        private static readonly Dictionary<int, HashSet<string>>
            WaitingRooms = new();

        // ==========================================
        // Disconnect
        // ==========================================


        public string Ping()
        {
            return "Pong";
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            foreach (var session in ActiveSessions)
            {
                if (session.Value.Remove(Context.ConnectionId))
                {
                    await Clients.Group(session.Key.ToString())
                        .SendAsync(
                            "UserLeft",
                            Context.ConnectionId
                        );

                    if (session.Value.Count == 0)
                    {
                        ActiveSessions.Remove(session.Key);
                        break;
                    }
                }
            }

            foreach (var room in WaitingRooms)
            {
                room.Value.Remove(Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ==========================================
        // Waiting Room
        // ==========================================

        public async Task RequestJoin(int sessionId)
        {
            if (!WaitingRooms.ContainsKey(sessionId))
            {
                WaitingRooms[sessionId] =
                    new HashSet<string>();
            }

            WaitingRooms[sessionId]
                .Add(Context.ConnectionId);

            // Waiting participants need session-ended notifications too, even
            // though they have not yet been admitted to the call.
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());

            await Clients.Group(sessionId.ToString())
                .SendAsync(
                    "UserWaiting",
                    Context.ConnectionId
                );
        }

        // ==========================================
        // Admit User
        // ==========================================

        public async Task AdmitUser(
            int sessionId,
            string connectionId)
        {
            if (WaitingRooms.ContainsKey(sessionId))
            {
                WaitingRooms[sessionId]
                    .Remove(connectionId);
            }

            await Clients.Client(connectionId)
                .SendAsync("Admitted");
        }

        // ================= JOIN SESSION =================

        

        public async Task JoinSession(int sessionId)
        {
            try
            {
                Console.WriteLine($"JoinSession: {sessionId}");

                _logger.LogInformation(
    "JoinSession called. SessionId={SessionId}, ConnectionId={ConnectionId}",
    sessionId,
    Context.ConnectionId);

                if (!ActiveSessions.ContainsKey(sessionId))
                ActiveSessions[sessionId] = new HashSet<string>();

            if (!ActiveSessions[sessionId].Contains(Context.ConnectionId))
            {
                ActiveSessions[sessionId].Add(Context.ConnectionId);
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());

            if (WaitingRooms.ContainsKey(sessionId))
            {
                WaitingRooms[sessionId].Remove(Context.ConnectionId);
            }

            // Send existing participants to the joining client
            await Clients.Caller.SendAsync(
                "ExistingParticipants",
                ActiveSessions[sessionId]
                    .Where(x => x != Context.ConnectionId)
                    .ToList());

                _logger.LogInformation("Added to ActiveSessions");

                // Send waiting users (for counselors)
                if (WaitingRooms.ContainsKey(sessionId))
            {
                foreach (var waitingUser in WaitingRooms[sessionId])
                {
                    await Clients.Caller.SendAsync(
                        "UserWaiting",
                        waitingUser);
                }
            }

            // Notify everyone else only AFTER this client
            // has successfully joined the SignalR group.
            await Clients.OthersInGroup(sessionId.ToString())
                .SendAsync(
                    "UserJoined",
                    Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JoinSession");
                throw;
            }
        }

        // ================= WEBRTC SIGNALING =================

        public async Task SendOffer(string targetId, string offer)
        {
            await Clients.Client(targetId)
                .SendAsync("ReceiveOffer", Context.ConnectionId, offer);
        }

        public async Task SendAnswer(string targetId, string answer)
        {
            await Clients.Client(targetId)
                .SendAsync("ReceiveAnswer", Context.ConnectionId, answer);
        }

        public async Task SendIceCandidate(string targetId, string candidate)
        {
            await Clients.Client(targetId)
                .SendAsync("ReceiveIceCandidate", Context.ConnectionId, candidate);
        }

        // ================= MODERATION =================

        public async Task MuteUser(string targetId)
        {
            await Clients.Client(targetId).SendAsync("ForceMute");
        }

        public async Task UnmuteUser(string targetId)
        {
            await Clients.Client(targetId).SendAsync("ForceUnmute");
        }

        public async Task RaiseHand(string sessionId)
        {
            await Clients.Group(sessionId)
                .SendAsync("UserRaisedHand", Context.ConnectionId);
        }

        public async Task EndSession(int sessionId)
        {
            var userIdValue = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var userId))
                throw new HubException("You must be signed in to end a session.");

            var session = await _context.Sessions
                .Include(item => item.Bookings)
                .FirstOrDefaultAsync(item => item.SessionId == sessionId);

            if (session is null)
                throw new HubException("Session not found.");

            if (!session.Bookings.Any(booking => booking.CounselorId == userId))
                throw new HubException("Only the assigned counselor can end this session.");

            if (!session.IsEnded)
            {
                session.IsEnded = true;
                session.EndedAt = DateTime.UtcNow;

                foreach (var booking in session.Bookings)
                {
                    booking.BookingStatus = "Completed";
                }

                await _context.SaveChangesAsync();
            }

            await Clients.Group(sessionId.ToString()).SendAsync("SessionEnded");
        }
    }
}
