using Microsoft.EntityFrameworkCore;
using PLC.User.API.Models;

namespace PLC.User.API.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    public DbSet<Models.User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<Models.User>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => e.KeycloakUserId)
                .IsUnique()
                .HasDatabaseName("IX_Users_KeycloakUserId");

            entity.HasIndex(e => e.Username)
                .HasDatabaseName("IX_Users_Username");

            entity.HasIndex(e => e.Email)
                .HasDatabaseName("IX_Users_Email");

            // Table name
            entity.ToTable("Users");

            // Configure properties
            entity.Property(e => e.Id)
                .HasDefaultValueSql("NEWID()");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });

        // Seed data
        modelBuilder.Entity<Models.User>().HasData(
            new Models.User
            {
                Id = Guid.Parse("51a0d936-35bc-4b73-b530-1579e5020c5b"),
                KeycloakUserId = Guid.Parse("f655ca4d-2dfb-4e16-aea1-9b01f26d734d"),
                Username = "admin",
                Email = "admin@plc.com",
                FirstName = "Admin",
                LastName = "User",
                Role = "admin",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Models.User
            {
                Id = Guid.Parse("c821ae30-32b9-4662-a842-39d58c06eda5"),
                KeycloakUserId = Guid.Parse("263fb5ae-10d6-4868-8363-ec9557961fe9"),
                Username = "testuser",
                Email = "testuser@plc.com",
                FirstName = "Test",
                LastName = "User",
                Role = "user",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
