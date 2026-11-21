using Microsoft.EntityFrameworkCore;
using TeamTaskBoard.Api.Data;
using TeamTaskBoard.Api.Models;
using TeamTaskBoard.Api.Services;
using Xunit;
using TaskStatus = TeamTaskBoard.Api.Models.TaskStatus;

namespace TeamTaskBoard.Api.Tests;

public sealed class OverdueTaskReportServiceTests
{
    [Fact]
    public async Task GroupsOnlyOverdueIncompleteTasksByAssignedMember()
    {
        await using var db = Seed();
        db.Tasks.AddRange(Task(1, "Old API", 2, TaskPriority.High, TaskStatus.ToDo, DateTime.UtcNow.AddHours(-2)), Task(2, "Old UI", 2, TaskPriority.Low, TaskStatus.InProgress, DateTime.UtcNow.AddDays(-1)), Task(3, "Future", 2, TaskPriority.Medium, TaskStatus.ToDo, DateTime.UtcNow.AddDays(1)), Task(4, "Done", 2, TaskPriority.High, TaskStatus.Completed, DateTime.UtcNow.AddDays(-1)), Task(5, "Owner task", 1, TaskPriority.Medium, TaskStatus.ToDo, DateTime.UtcNow.AddMinutes(-5)));
        await db.SaveChangesAsync();
        var report = await new OverdueTaskReportService(db).GetAsync(10, "owner", CancellationToken.None);
        Assert.NotNull(report); Assert.Equal(3, report!.TotalOverdueTaskCount); Assert.Equal(2, report.Members.Count); Assert.Equal(2, report.Members.Single(member => member.AssignedUserId == 2).OverdueTaskCount); Assert.Single(report.Members.Single(member => member.AssignedUserId == 1).Tasks); Assert.DoesNotContain(report.Members.SelectMany(member => member.Tasks), task => task.Title is "Future" or "Done");
    }

    [Fact]
    public async Task NonOwnerCannotReadReport()
    {
        await using var db = Seed();
        Assert.Null(await new OverdueTaskReportService(db).GetAsync(10, "member", CancellationToken.None));
    }

    [Fact]
    public async Task ReturnsEmptyReportWhenNoTasksAreOverdue()
    {
        await using var db = Seed(); db.Tasks.Add(Task(1, "Future", 2, TaskPriority.Low, TaskStatus.ToDo, DateTime.UtcNow.AddDays(1))); await db.SaveChangesAsync();
        var report = await new OverdueTaskReportService(db).GetAsync(10, "owner", CancellationToken.None);
        Assert.NotNull(report); Assert.Equal(0, report!.TotalOverdueTaskCount); Assert.Empty(report.Members);
    }

    private static TaskItem Task(long id, string title, long assignedUserId, TaskPriority priority, TaskStatus status, DateTime dueDateUtc) => new() { Id = id, ProjectId = 10, Title = title, AssignedUserId = assignedUserId, Priority = priority, Status = status, DueDateUtc = dueDateUtc, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
    private static AppDbContext Seed()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options; var db = new AppDbContext(options);
        db.Users.AddRange(new User { Id = 1, FirebaseUid = "owner", Email = "owner@example.com", DisplayName = "Owner" }, new User { Id = 2, FirebaseUid = "member", Email = "member@example.com", DisplayName = "Member" }); db.Projects.Add(new Project { Id = 10, Name = "Project", OwnerId = 1, Members = new List<ProjectMember> { new() { UserId = 2 } } }); db.SaveChanges(); return db;
    }
}
