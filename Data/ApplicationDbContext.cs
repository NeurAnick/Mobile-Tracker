using Microsoft.EntityFrameworkCore;
using MobileTracker.Models;

namespace MobileTracker.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<LostPhoneReport> LostPhoneReports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LostPhoneReport>()
                .HasIndex(r => r.IMEI)
                .IsUnique();
        }
    }
}