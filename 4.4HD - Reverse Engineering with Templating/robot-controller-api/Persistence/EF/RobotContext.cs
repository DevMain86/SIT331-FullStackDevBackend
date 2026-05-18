using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace robot_controller_api.Persistence
{
    public partial class RobotContext : DbContext
    {
        public RobotContext() { }
        public RobotContext(DbContextOptions<RobotContext> options) : base(options) { }

        public virtual DbSet<RobotCommand> RobotCommands { get; set; } = null!;
        public virtual DbSet<Map> Maps { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Use environment variable DB_CONNECTION if present, otherwise fallback to local
                var conn = Environment.GetEnvironmentVariable("DB_CONNECTION")
                           ?? "Host=localhost; Database=sit331; Username=postgres; Password=";

                optionsBuilder
                    .UseNpgsql(conn)
                    .LogTo(Console.Write) // writes EF SQL and events to console
                    .EnableSensitiveDataLogging();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Minimal mapping scaffolded by dotnet-ef; adjust if your scaffold produced different names.
            modelBuilder.Entity<RobotCommand>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("robotcommand");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.IsMoveCommand).HasColumnName("ismovecommand");
                entity.Property(e => e.CreatedDate).HasColumnName("createddate");
                entity.Property(e => e.ModifiedDate).HasColumnName("modifieddate");
            });

            modelBuilder.Entity<Map>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("map");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.Rows).HasColumnName("rows");
                entity.Property(e => e.Columns).HasColumnName("columns");
                entity.Property<bool>("issquare").HasColumnName("issquare");
                entity.Property(e => e.CreatedDate).HasColumnName("createddate");
                entity.Property(e => e.ModifiedDate).HasColumnName("modifieddate");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
