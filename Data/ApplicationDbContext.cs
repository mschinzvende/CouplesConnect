using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CouplesConnect.Models;
using CouplesConnect.Data.Models;

namespace CouplesConnect.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Counselor> Counselors { get; set; }
        public DbSet<CounselorCredential> CounselorCredentials { get; set; }
        public DbSet<CounselorAvailability> CounselorAvailabilities { get; set; }

        public DbSet<Service> Services { get; set; }

        public DbSet<Booking> Bookings { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<Session> Sessions { get; set; }

        public DbSet<SessionNote> SessionNotes { get; set; }

        public DbSet<Feedback> Feedback { get; set; }

        public DbSet<Message> Messages { get; set; }

        public DbSet<UserGoal> UserGoals { get; set; }

        public DbSet<ReflectionNote> ReflectionNotes { get; set; }

        public DbSet<Resource> Resources { get; set; }

        public DbSet<SupportTicket> SupportTickets { get; set; }

        public DbSet<UserSubscription> UserSubscriptions { get; set; }
    }
}