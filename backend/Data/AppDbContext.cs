
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
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

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

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("Projects");
            entity.HasKey(project => project.Id);
            entity.Property(project => project.Name).IsRequired().HasMaxLength(150);
            entity.Property(project => project.Description).IsRequired().HasMaxLength(2000);
            entity.Property(project => project.CreatedAtUtc).IsRequired();
            entity.Property(project => project.UpdatedAtUtc).IsRequired();
            entity.HasIndex(project => project.OwnerId);
            entity.HasOne(project => project.Owner)
                .WithMany(user => user.OwnedProjects)
                .HasForeignKey(project => project.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.ToTable("ProjectMembers");
            entity.HasKey(member => new { member.ProjectId, member.UserId });
            entity.HasOne(member => member.Project)
                .WithMany(project => project.Members)
                .HasForeignKey(member => member.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(member => member.User)
                .WithMany(user => user.ProjectMemberships)
                .HasForeignKey(member => member.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(member => member.UserId);
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasKey(task => task.Id);
            entity.Property(task => task.Title).IsRequired().HasMaxLength(150);
            entity.Property(task => task.Description).IsRequired().HasMaxLength(2000);
            entity.Property(task => task.Priority).IsRequired();
            entity.Property(task => task.Status).IsRequired();
            entity.Property(task => task.CreatedAtUtc).IsRequired();
            entity.Property(task => task.UpdatedAtUtc).IsRequired();
            entity.HasIndex(task => task.ProjectId);
            entity.HasIndex(task => task.AssignedUserId);
            entity.HasIndex(task => task.Status);
            entity.HasIndex(task => task.Priority);
            entity.HasOne(task => task.Project)
                .WithMany(project => project.Tasks)
                .HasForeignKey(task => task.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(task => task.AssignedUser)
                .WithMany(user => user.AssignedTasks)
                .HasForeignKey(task => task.AssignedUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

