using Microsoft.EntityFrameworkCore;
using TeamTaskBoard.Api.Controllers;
using TeamTaskBoard.Api.Data;
using TeamTaskBoard.Api.Models;
using TeamTaskBoard.Api.Services;
using Xunit;

namespace TeamTaskBoard.Api.Tests;

public sealed class ProjectServiceTests
{
    [Fact]
    public async Task GetAvailableAsync_ReturnsOwnedAndMemberProjectsOnly()
    {
        await using var context = CreateContext();
        context.Users.AddRange(
            new User { Id = 1, FirebaseUid = "owner-uid", Email = "owner@example.com", DisplayName = "Owner" },
            new User { Id = 2, FirebaseUid = "member-uid", Email = "member@example.com", DisplayName = "Member" },
            new User { Id = 3, FirebaseUid = "other-uid", Email = "other@example.com", DisplayName = "Other" });
        context.Projects.AddRange(
            new Project { Id = 1, Name = "Owned", OwnerId = 1 },
            new Project { Id = 2, Name = "Joined", OwnerId = 3, Members = new List<ProjectMember> { new() { UserId = 2 } } },
            new Project { Id = 3, Name = "Hidden", OwnerId = 3 });
        await context.SaveChangesAsync();

        var projects = await new ProjectService(context).GetAvailableAsync("member-uid", CancellationToken.None);

        Assert.Equal(new[] { "Joined" }, projects.Select(project => project.Name));
    }

    [Fact]
    public async Task UpdateAndDelete_AreRestrictedToOwner()
    {
        await using var context = CreateContext();
        context.Users.AddRange(
            new User { Id = 1, FirebaseUid = "owner-uid", Email = "owner@example.com", DisplayName = "Owner" },
            new User { Id = 2, FirebaseUid = "member-uid", Email = "member@example.com", DisplayName = "Member" });
        context.Projects.Add(new Project { Id = 1, Name = "Owned", OwnerId = 1 });
        await context.SaveChangesAsync();
        var service = new ProjectService(context);

        var updated = await service.UpdateAsync(1, "member-uid", new UpdateProjectRequest { Name = "Changed" }, CancellationToken.None);
        var deleted = await service.DeleteAsync(1, "member-uid", CancellationToken.None);

        Assert.Null(updated);
        Assert.False(deleted);
        Assert.Equal("Owned", (await context.Projects.SingleAsync()).Name);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
