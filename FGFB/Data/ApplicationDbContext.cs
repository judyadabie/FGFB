using FGFB.Models;
using Microsoft.EntityFrameworkCore;

namespace FGFB.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<League> Leagues { get; set; }
        public DbSet<LeagueRegistration> LeagueRegistrations { get; set; }
        public DbSet<EventRegistration> EventRegistrations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LeagueRegistration>()
        .ToTable(tb => tb.HasTrigger("trg_CloseLeagueWhenFull"));
            modelBuilder.Entity<LeagueRegistration>()
                .ToTable(tb => tb.UseSqlOutputClause(false));
            modelBuilder.Entity<EventRegistration>(entity =>
            {
                entity.ToTable("EventRegistrations");

                entity.HasKey(e => e.EventRegistrationId);

                entity.Property(e => e.EventRegistrationId)
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.EventName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(320);

                entity.Property(e => e.LeagueLevel)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.LeagueDisplayName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.BaseTicketPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.LeagueFee)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.ProcessingFee)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.TotalPaid)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.PaymentStatus)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("Pending");

                entity.Property(e => e.StripeSessionId)
                    .HasMaxLength(255);

                entity.Property(e => e.StripePaymentIntentId)
                    .HasMaxLength(255);

                entity.Property(e => e.CreatedAtUtc)
                    .HasColumnType("datetime2")
                    .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasIndex(e => e.Email)
                    .HasDatabaseName("IX_EventRegistrations_Email");

                entity.HasIndex(e => e.StripeSessionId)
                    .IsUnique()
                    .HasFilter("[StripeSessionId] IS NOT NULL")
                    .HasDatabaseName("IX_EventRegistrations_StripeSessionId");

                entity.HasIndex(e => new { e.EventName, e.Email })
                    .IsUnique()
                    .HasDatabaseName("IX_EventRegistrations_EventName_Email");
            });
        }
    }
}