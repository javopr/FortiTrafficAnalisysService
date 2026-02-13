using Microsoft.EntityFrameworkCore;
using FortiTrafficAnalysis.Domain.Entities;
using System;

namespace FortiTrafficAnalysis.Data
{
    /// <summary>
    /// Main database context for FortiGate Traffic Analysis Service
    /// Author: javier.morales@intwo.cloud
    /// Organization: INTEGRATION TECHNOLOGIES CORP.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for all entities
        public DbSet<AppGroup> AppGroups { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<FTAService> FTAServices { get; set; }
        public DbSet<FortiGate> FortiGates { get; set; }
        public DbSet<TrafficLog> TrafficLogs { get; set; }
        public DbSet<TrafficAnalysis> TrafficAnalyses { get; set; }
        public DbSet<TrafficAnalysisRecommendation> TrafficAnalysisRecommendations { get; set; }
        public DbSet<AIConversation> AIConversations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints
            ConfigureAppGroup(modelBuilder);
            ConfigureAppUser(modelBuilder);
            ConfigureCustomer(modelBuilder);
            ConfigureFTAService(modelBuilder);
            ConfigureFortiGate(modelBuilder);
            ConfigureTrafficLog(modelBuilder);
            ConfigureTrafficAnalysis(modelBuilder);
            ConfigureTrafficAnalysisRecommendation(modelBuilder);
            ConfigureAIConversation(modelBuilder);

            // Seed initial data
            SeedData(modelBuilder);
        }

        private void ConfigureAppGroup(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppGroup>(entity =>
            {
                entity.HasKey(e => e.AppGroupID);
                entity.Property(e => e.AppGroupName).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.AppGroupName).IsUnique();
            });
        }

        private void ConfigureAppUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.HasKey(e => e.AppAccessID);
                entity.Property(e => e.UserUPN).IsRequired().HasMaxLength(255);
                entity.Property(e => e.AppUserName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.AppUserEmail).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PasswordHash).HasMaxLength(500);
                entity.HasIndex(e => e.UserUPN).IsUnique();

                entity.HasOne(e => e.AppGroup)
                    .WithMany(g => g.AppUsers)
                    .HasForeignKey(e => e.AppGroupID)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureCustomer(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.CustomerID);
                entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.CustomerName).IsUnique();
            });
        }

        private void ConfigureFTAService(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FTAService>(entity =>
            {
                entity.HasKey(e => e.FTAID);
                entity.Property(e => e.JobID).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ServiceStatus).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.JobID).IsUnique();

                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.FTAServices)
                    .HasForeignKey(e => e.CustomerID)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureFortiGate(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FortiGate>(entity =>
            {
                entity.HasKey(e => e.FGID);
                entity.Property(e => e.FGHostname).IsRequired().HasMaxLength(50);
                entity.Property(e => e.FGHost).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FGSerial).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FGvDOM).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FGapiKey).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FGStatus).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.FGSerial).IsUnique();

                entity.HasOne(e => e.FTAService)
                    .WithMany(s => s.FortiGates)
                    .HasForeignKey(e => e.FTAID)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureTrafficLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TrafficLog>(entity =>
            {
                entity.HasKey(e => e.TrafficLogID);

                entity.HasOne(e => e.TrafficAnalysis)
                    .WithMany(ta => ta.TrafficLogs)
                    .HasForeignKey(e => e.TrafficAnalysisID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.FortiGate)
                    .WithMany(f => f.TrafficLogs)
                    .HasForeignKey(e => e.FGID)
                    .OnDelete(DeleteBehavior.SetNull);

                // Create indexes for common query patterns
                entity.HasIndex(e => e.TrafficAnalysisID);
                entity.HasIndex(e => e.LogDate);
                entity.HasIndex(e => e.SrcIP);
                entity.HasIndex(e => e.DstIP);
                entity.HasIndex(e => e.Action);
            });
        }

        private void ConfigureTrafficAnalysis(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TrafficAnalysis>(entity =>
            {
                entity.HasKey(e => e.TrafficAnalysisID);
                entity.Property(e => e.TicketNumber).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Summary).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.CreatedByUPN).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);

                entity.HasIndex(e => e.TicketNumber).IsUnique();

                entity.HasOne(e => e.FortiGate)
                    .WithMany()
                    .HasForeignKey(e => e.FGID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.FTAService)
                    .WithMany()
                    .HasForeignKey(e => e.FTAID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedDate);
                entity.HasIndex(e => e.CreatedByUPN);
            });
        }

        private void ConfigureTrafficAnalysisRecommendation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TrafficAnalysisRecommendation>(entity =>
            {
                entity.HasKey(e => e.TrafficAnalysisRecommendationID);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.RecommendationText).IsRequired();
                entity.Property(e => e.CreatedByUPN).HasMaxLength(255);

                entity.HasOne(e => e.TrafficAnalysis)
                    .WithMany(ta => ta.Recommendations)
                    .HasForeignKey(e => e.TrafficAnalysisID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.TrafficAnalysisID);
                entity.HasIndex(e => e.CreatedDate);
            });
        }

        private void ConfigureAIConversation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AIConversation>(entity =>
            {
                entity.HasKey(e => e.ConversationID);
                entity.Property(e => e.UserQuestion).IsRequired();
                entity.Property(e => e.AIResponse).IsRequired();
                entity.Property(e => e.CreatedByUPN).IsRequired().HasMaxLength(255);
                entity.Property(e => e.CreatedDate).IsRequired();

                entity.HasOne(e => e.TrafficAnalysis)
                    .WithMany()
                    .HasForeignKey(e => e.TrafficAnalysisID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.TrafficAnalysisID);
                entity.HasIndex(e => e.CreatedDate);
                entity.HasIndex(e => e.CreatedByUPN);
            });
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed AppGroups
            var usersGroupId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var adminsGroupId = Guid.Parse("22222222-2222-2222-2222-222222222222");

            modelBuilder.Entity<AppGroup>().HasData(
                new AppGroup
                {
                    AppGroupID = usersGroupId,
                    AppGroupName = "Users"
                },
                new AppGroup
                {
                    AppGroupID = adminsGroupId,
                    AppGroupName = "Admins"
                }
            );

            // Seed default admin user
            // Username: admin@fgtas.local
            // Password: Admin@123
            // Password hash using SHA256
            var adminUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            modelBuilder.Entity<AppUser>().HasData(
                new AppUser
                {
                    AppAccessID = adminUserId,
                    UserUPN = "admin@fgtas.local",
                    AppGroupID = adminsGroupId,
                    AppUserName = "System Administrator",
                    AppUserEmail = "admin@fgtas.local",
                    PasswordHash = "jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=" // Admin@123 hashed with SHA256
                }
            );
        }
    }
}
