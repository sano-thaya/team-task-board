using Microsoft.EntityFrameworkCore;
using TeamTaskBoard.Api.Controllers;
using TeamTaskBoard.Api.Data;
using TeamTaskBoard.Api.Models;
using TeamTaskBoard.Api.Services;
using Xunit;
using TaskState = TeamTaskBoard.Api.Models.TaskStatus;

namespace TeamTaskBoard.Api.Tests;

public sealed class TaskServiceTests
{
    [Fact]
    public async Task CreateListSearchAndFilter_WorksForProjectMember()
    {
        await using var db = Seed(); var service = new TaskService(db);
        var created = await service.CreateAsync(10, "member", new CreateTaskRequest { Title = "Fix login", AssignedUserId = 2, Priority = TaskPriority.High }, CancellationToken.None);
        await service.CreateAsync(10, "member", new CreateTaskRequest { Title = "Write docs", AssignedUserId = 2, Priority = TaskPriority.Low }, CancellationToken.None);

        var result = await service.GetAllAsync(10, "member", "login", TaskPriority.High, CancellationToken.None);

        Assert.NotNull(result); Assert.Single(result!); Assert.Equal(created.Id, result[0].Id);
    }

    [Fact]
    public async Task Create_RejectsInvalidTitlePriorityAndNonMemberAssignee()
    {
        await using var db = Seed(); var service = new TaskService(db);
        await Assert.ThrowsAsync<TaskValidationException>(() => service.CreateAsync(10, "member", new CreateTaskRequest { Title = " ", AssignedUserId = 2 }, CancellationToken.None));
        await Assert.ThrowsAsync<TaskValidationException>(() => service.CreateAsync(10, "member", new CreateTaskRequest { Title = "Bad priority", AssignedUserId = 3, Priority = (TaskPriority)99 }, CancellationToken.None));
        await Assert.ThrowsAsync<TaskValidationException>(() => service.CreateAsync(10, "member", new CreateTaskRequest { Title = "Bad assignee", AssignedUserId = 3 }, CancellationToken.None));
        await Assert.ThrowsAsync<TaskValidationException>(() => service.CreateAsync(10, "member", new CreateTaskRequest { Title = "Past deadline", AssignedUserId = 2, DueDateUtc = DateTime.UtcNow.Date.AddDays(-1) }, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateDeleteAndUnauthorizedAccess_AreEnforced()
    {
        await using var db = Seed(); var service = new TaskService(db);
        var task = await service.CreateAsync(10, "member", new CreateTaskRequest { Title = "Original", AssignedUserId = 2 }, CancellationToken.None);
        var updated = await service.UpdateAsync(10, task.Id, "member", new UpdateTaskRequest { Title = "Updated", AssignedUserId = 2, Priority = TaskPriority.Medium, Status = TaskState.Completed }, CancellationToken.None);
        Assert.Equal("Updated", updated!.Title); Assert.Equal(TaskState.Completed, updated.Status);
        Assert.Null(await service.GetAsync(10, task.Id, "other", CancellationToken.None));
        Assert.True(await service.DeleteAsync(10, task.Id, "member", CancellationToken.None));
        Assert.Null(await service.GetAsync(10, task.Id, "member", CancellationToken.None));
    }

    private static AppDbContext Seed()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);
        db.Users.AddRange(new User { Id = 1, FirebaseUid = "owner", Email = "owner@example.com", DisplayName = "Owner" }, new User { Id = 2, FirebaseUid = "member", Email = "member@example.com", DisplayName = "Member" }, new User { Id = 3, FirebaseUid = "other", Email = "other@example.com", DisplayName = "Other" });
        db.Projects.Add(new Project { Id = 10, Name = "Project", OwnerId = 1, Members = new List<ProjectMember> { new() { UserId = 2 } } });
        db.SaveChanges(); return db;
    }
}
