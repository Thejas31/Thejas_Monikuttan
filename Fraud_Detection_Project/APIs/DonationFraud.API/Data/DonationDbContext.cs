using DonationFraud.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace DonationFraud.API.Data
{
    public class DonationDbContext : DbContext
    {
        public DonationDbContext(DbContextOptions<DonationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<Donation> Donations { get; set; }
        public DbSet<FraudFlag> FraudFlags { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<FraudRuleConfig> FraudRuleConfigs { get; set; }
        public DbSet<DeviceFingerprint> DeviceFingerprints { get; set; }
        public DbSet<IpIntelligence> IpIntelligences { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<MlPrediction> MlPredictions { get; set; }
        public DbSet<MlModelMetadata> MlModelMetadata { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Donation>()
                .HasOne(d => d.Campaign)
                .WithMany(c => c.Donations)
                .HasForeignKey(d => d.CampaignId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Donation>()
                .HasOne(d => d.User)
                .WithMany(u => u.Donations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Donation>()
                .HasOne(d => d.DeviceFingerprint)
                .WithMany()
                .HasForeignKey(d => d.DeviceFingerprintId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Donation>()
                .HasOne(d => d.IpIntelligence)
                .WithMany()
                .HasForeignKey(d => d.IpIntelligenceId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Donation>()
                .HasOne(d => d.PaymentMethod)
                .WithMany()
                .HasForeignKey(d => d.PaymentMethodId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<FraudFlag>()
                .HasOne(f => f.Donation)
                .WithOne(d => d.FraudFlag)
                .HasForeignKey<FraudFlag>(f => f.DonationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MlPrediction>()
                .HasOne(p => p.Donation)
                .WithMany()
                .HasForeignKey(p => p.DonationId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
                
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<PaymentMethod>()
                .HasIndex(p => p.Fingerprint);
        }
    }
}
