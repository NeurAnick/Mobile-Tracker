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

        public DbSet<Thana> Thanas { get; set; }

        public DbSet<GDInfo> GDInfos { get; set; }
        public DbSet<PolicyAcceptance> PolicyAcceptances { get; set; }

        public DbSet<CaseStatusHistory> CaseStatusHistories { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LostPhoneReport>()
                .HasIndex(r => r.IMEI)
                .IsUnique();
        }
    }
}