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

        // Seed data
        modelBuilder.Entity<Models.User>().HasData(
            new Models.User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@plc.com",
                FullName = "Administrator",
                Department = "IT",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Models.User
            {
                Id = 2,
                Username = "testuser",
                Email = "testuser@plc.com",
                FullName = "Test User",
                Department = "Engineering",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}
