using Microsoft.AspNetCore.SignalR;

namespace CouplesConnect.Hubs
{
    public class SessionHub : Hub
    {
        private static readonly Dictionary<string, HashSet<string>> ActiveSessions = new();
        private static readonly Dictionary<string, HashSet<string>> WaitingRooms = new();

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            foreach (var session in ActiveSessions)
            {
                if (session.Value.Remove(Context.ConnectionId))
                {
                    await Clients.Group(session.Key)
                        .SendAsync("UserLeft", Context.ConnectionId);
                }
            }

            foreach (var room in WaitingRooms.Values)
            {
                room.Remove(Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ================= WAITING ROOM =================
        public async Task RequestJoin(string sessionId)
        {
            if (!WaitingRooms.ContainsKey(sessionId))
                WaitingRooms[sessionId] = new HashSet<string>();

            WaitingRooms[sessionId].Add(Context.ConnectionId);

            // Notify therapist
            await Clients.Group(sessionId)
                .SendAsync("UserWaiting", Context.ConnectionId);
        }

        public async Task AdmitUser(string sessionId, string connectionId)
        {
            if (!ActiveSessions.ContainsKey(sessionId))
                ActiveSessions[sessionId] = new HashSet<string>();

            ActiveSessions[sessionId].Add(connectionId);

            await Groups.AddToGroupAsync(connectionId, sessionId);

            await Clients.Client(connectionId)
                .SendAsync("Admitted");

            await Clients.Group(sessionId)
                .SendAsync("UserJoined", connectionId);
        }

        // ================= JOIN SESSION =================
        public async Task JoinSession(string sessionId)
        {
            if (!ActiveSessions.ContainsKey(sessionId))
                ActiveSessions[sessionId] = new HashSet<string>();

            ActiveSessions[sessionId].Add(Context.ConnectionId);

            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

            await Clients.Caller.SendAsync(
                "ExistingParticipants",
                ActiveSessions[sessionId].Where(x => x != Context.ConnectionId)
            );

            await Clients.OthersInGroup(sessionId)
                .SendAsync("UserJoined", Context.ConnectionId);
        }

        // ================= WEBRTC =================
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
    }
}