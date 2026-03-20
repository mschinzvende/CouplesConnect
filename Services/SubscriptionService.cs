using CouplesConnect.Data;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Services
{
    public class SubscriptionService
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasSessions(Guid userId)
        {
            var sub = await _context.UserSubscriptions
                .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);

            if (sub == null) return false;

            return sub.SessionsRemaining > 0;
        }

        public async Task<bool> ConsumeSession(Guid userId)
        {
            var sub = await _context.UserSubscriptions
                .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);

            if (sub == null || sub.SessionsRemaining <= 0)
                return false;

            sub.SessionsRemaining--;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}