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
            try
            {
                var sub = await _context.UserSubscriptions
                        .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);

                if (sub == null || sub.SessionsRemaining <= 0)
                    return false;

                sub.SessionsRemaining--;

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<bool> RestoreSession(Guid userId, int? subscriptionId)
        {

            try
            {
                if (!subscriptionId.HasValue)
                {
                    return false;
                }
                var sub = await _context.UserSubscriptions
                    .FirstOrDefaultAsync(x => x.UserId == userId && x.SubscriptionId == subscriptionId);

                if (sub == null)
                    return false;

                sub.SessionsRemaining = sub.SessionsRemaining + 1;
                sub.IsActive = true;

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}