using Microsoft.AspNetCore.SignalR;

namespace CouplesConnect.Hubs
{
    public class SessionHub : Hub
    {
        public async Task JoinSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        }

        public async Task SendOffer(string sessionId, string offer)
        {
            await Clients.OthersInGroup(sessionId).SendAsync("ReceiveOffer", offer);
        }

        public async Task SendAnswer(string sessionId, string answer)
        {
            await Clients.OthersInGroup(sessionId).SendAsync("ReceiveAnswer", answer);
        }

        public async Task SendIceCandidate(string sessionId, string candidate)
        {
            await Clients.OthersInGroup(sessionId).SendAsync("ReceiveIceCandidate", candidate);
        }

        public async Task NotifyJoin(string sessionId)
        {
            await Clients.OthersInGroup(sessionId).SendAsync("UserJoined");
        }
    }
}