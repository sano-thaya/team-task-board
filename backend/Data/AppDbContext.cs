
using Microsoft.EntityFrameworkCore;
using TeamTaskBoard.Api.Models;

namespace TeamTaskBoard.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.FirebaseUid)
                .IsRequired()
                .HasMaxLength(128);

            entity.HasIndex(user => user.FirebaseUid)
                .IsUnique();

            entity.Property(user => user.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.HasIndex(user => user.Email)
                .IsUnique();

            entity.Property(user => user.DisplayName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(user => user.Role)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Member");

            entity.Property(user => user.CreatedAtUtc)
                .IsRequired();
        });
    }
}

