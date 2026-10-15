using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartGymManagement.Models;

namespace SmartGymManagement.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Member> Members { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<MembershipPackage> MembershipPackages { get; set; }
        public DbSet<Membership> Memberships { get; set; }
        public DbSet<ClassSchedule> ClassSchedules { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<BMIRecord> BMIRecords { get; set; }
        public DbSet<ChatbotMessage> ChatbotMessages { get; set; }
        public DbSet<Room> Rooms { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Preserve the existing Identity schema in design-time and runtime models.
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().Property(x => x.LoginProvider).HasMaxLength(128);
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().Property(x => x.ProviderKey).HasMaxLength(128);
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().Property(x => x.LoginProvider).HasMaxLength(128);
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().Property(x => x.Name).HasMaxLength(128);

            builder.Entity<MembershipPackage>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            builder.Entity<PaymentTransaction>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            builder.Entity<Invoice>()
                .Property(i => i.TotalAmount)
                .HasPrecision(18, 2);

            builder.Entity<Trainer>()
                .HasIndex(t => t.Email)
                .IsUnique();

            builder.Entity<Trainer>()
                .HasIndex(t => t.Phone)
                .IsUnique();

            builder.Entity<Attendance>()
                .HasOne(a => a.Booking)
                .WithMany()
                .HasForeignKey(a => a.BookingId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Attendance>()
                .HasOne(a => a.Member)
                .WithMany(m => m.Attendances)
                .HasForeignKey(a => a.MemberId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Attendance>()
                .HasOne(a => a.ClassSchedule)
                .WithMany()
                .HasForeignKey(a => a.ClassId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<BMIRecord>()
                .Property(b => b.HeightCm)
                .HasPrecision(18, 2);

            builder.Entity<BMIRecord>()
                .Property(b => b.WeightKg)
                .HasPrecision(18, 2);

            builder.Entity<BMIRecord>()
                .Property(b => b.BMIValue)
                .HasPrecision(18, 2);

            builder.Entity<ChatbotMessage>()
                .HasOne(c => c.Member)
                .WithMany(m => m.ChatbotMessages)
                .HasForeignKey(c => c.MemberId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Attendance>().HasIndex(a => a.BookingId).IsUnique();
            builder.Entity<Booking>().HasIndex(b => new { b.MemberId, b.ClassId })
                .IsUnique().HasFilter("[Status] IN (0, 2)");
            builder.Entity<Invoice>().HasIndex(i => i.InvoiceNumber).IsUnique();
            builder.Entity<PaymentTransaction>().HasIndex(p => p.TransactionCode)
                .IsUnique().HasFilter("[TransactionCode] IS NOT NULL");
            builder.Entity<Membership>().Property(m => m.PurchasedPrice).HasPrecision(18, 2);
            builder.Entity<Membership>().Property(m => m.PurchasedPackageName).HasMaxLength(100);
            builder.Entity<Booking>().HasIndex(b => new { b.ClassId, b.Status });
            builder.Entity<ClassSchedule>().HasIndex(c => new { c.Status, c.StartTime });
            builder.Entity<BMIRecord>().HasIndex(b => new { b.MemberId, b.RecordedAt });
            builder.Entity<ChatbotMessage>().HasIndex(c => new { c.MemberId, c.CreatedAt });
        }
    }
}
