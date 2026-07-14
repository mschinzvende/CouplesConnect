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

        public DbSet<CounselorProfile> CounselorProfiles { get; set; }

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

        public DbSet<ResourceCategory> ResourceCategories { get; set; }

        public DbSet<SupportTicket> SupportTickets { get; set; }

        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<PlatformSetting> PlatformSettings { get; set; }

        public DbSet<ServiceType> ServiceTypes { get; set; }

        public DbSet<Package> Packages { get; set; }

     
        public DbSet<SessionFeedback> SessionFeedbacks { get; set; }
        public DbSet<CounselorEarning> CounselorEarnings { get; set; }

        public DbSet<BookingParticipant> BookingParticipants { get; set; }

        public DbSet<CounselorService> CounselorServices { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<CounselorProfile>()
                .HasOne(c => c.User)
                .WithOne(u => u.CounselorProfile)
                .HasForeignKey<CounselorProfile>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Booking → User
            builder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Booking → Counselor
            builder.Entity<Booking>()
                .HasOne(b => b.Counselor)
                .WithMany()
                .HasForeignKey(b => b.CounselorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Booking → Service
            builder.Entity<Booking>()
                .HasOne(b => b.Service)
                .WithMany()
                .HasForeignKey(b => b.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            
        }
    }
}