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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<League>()
                .HasQueryFilter(l => l.Status == LeagueStatus.Open);

            base.OnModelCreating(modelBuilder);
        }
    }
}