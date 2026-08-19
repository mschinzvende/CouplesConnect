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

        public DbSet<ContactRequest> ContactRequests { get; set; }

        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<PlatformSetting> PlatformSettings { get; set; }

        public DbSet<ServiceType> ServiceTypes { get; set; }

        public DbSet<Package> Packages { get; set; }

     
        public DbSet<SessionFeedback> SessionFeedbacks { get; set; }
        public DbSet<CounselorEarning> CounselorEarnings { get; set; }

        public DbSet<BookingParticipant> BookingParticipants { get; set; }

        public DbSet<CounselorService> CounselorServices { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<NotificationLog> NotificationLogs { get; set; }

        public DbSet<ForumCategory> ForumCategories { get; set; }

        public DbSet<ForumTopic> ForumTopics { get; set; }

        public DbSet<ForumReply> ForumReplies { get; set; }


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

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Notification>()
    .HasIndex(x => x.UserId);

            builder.Entity<Notification>()
                .HasIndex(x => x.IsRead);

            builder.Entity<Notification>()
                .HasIndex(x => x.CreatedDate);

            builder.Entity<NotificationLog>()
                .HasIndex(x => x.CreatedDate);

            builder.Entity<NotificationLog>()
                .HasIndex(x => x.Channel);

            builder.Entity<NotificationLog>()
                .HasIndex(x => x.Status);

            builder.Entity<ContactRequest>(entity =>
            {
                entity.HasIndex(x => new { x.Status, x.CreatedAt });
                entity.HasIndex(x => new { x.Email, x.CreatedAt });
            });

            builder.Entity<ForumCategory>(entity =>
            {
                entity.HasIndex(x => x.Name).IsUnique();
                entity.HasIndex(x => x.Slug).IsUnique();
                entity.HasIndex(x => new { x.IsActive, x.DisplayOrder });
            });

            builder.Entity<ForumTopic>(entity =>
            {
                entity.HasOne(x => x.Category)
                    .WithMany(x => x.Topics)
                    .HasForeignKey(x => x.ForumCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Author)
                    .WithMany()
                    .HasForeignKey(x => x.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.RowVersion).IsRowVersion();
                entity.HasIndex(x => new { x.ForumCategoryId, x.IsDeleted, x.IsArchived, x.IsPinned, x.LastActivityAt });
                entity.HasIndex(x => new { x.AuthorId, x.CreatedAt });
            });

            builder.Entity<ForumReply>(entity =>
            {
                entity.HasOne(x => x.Topic)
                    .WithMany(x => x.Replies)
                    .HasForeignKey(x => x.ForumTopicId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Author)
                    .WithMany()
                    .HasForeignKey(x => x.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.RowVersion).IsRowVersion();
                entity.HasIndex(x => new { x.ForumTopicId, x.IsDeleted, x.CreatedAt });
                entity.HasIndex(x => new { x.AuthorId, x.CreatedAt });
            });
        }
    }
}
