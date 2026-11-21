using Microsoft.EntityFrameworkCore;
using TeamTaskBoard.Api.Data;
using TeamTaskBoard.Api.Models;
using TaskState = TeamTaskBoard.Api.Models.TaskStatus;

namespace TeamTaskBoard.Api.Services;

public interface IOverdueTaskReportService
{
    Task<OverdueTaskReportResponse?> GetAsync(long projectId, string firebaseUid, CancellationToken cancellationToken);
}

public sealed class OverdueTaskReportService(AppDbContext db) : IOverdueTaskReportService
{
    public async Task<OverdueTaskReportResponse?> GetAsync(long projectId, string firebaseUid, CancellationToken cancellationToken)
    {
        var isOwner = await db.Projects.AsNoTracking().AnyAsync(project => project.Id == projectId && project.Owner.FirebaseUid == firebaseUid, cancellationToken);
        if (!isOwner) return null;

        var nowUtc = DateTime.UtcNow;
        var tasks = await db.Tasks.AsNoTracking().Include(task => task.AssignedUser)
            .Where(task => task.ProjectId == projectId && task.DueDateUtc.HasValue && task.DueDateUtc.Value < nowUtc && task.Status != TaskState.Completed)
            .OrderBy(task => task.AssignedUser.DisplayName).ThenBy(task => task.DueDateUtc).ThenBy(task => task.Id)
            .ToListAsync(cancellationToken);

        var members = tasks.GroupBy(task => new { task.AssignedUserId, task.AssignedUser.DisplayName })
            .Select(group => new OverdueTaskMemberResponse(group.Key.AssignedUserId, group.Key.DisplayName, group.Count(), group.Select(Map).ToList()))
            .ToList();
        return new OverdueTaskReportResponse(projectId, tasks.Count, members);
    }

    private static OverdueTaskResponse Map(TaskItem task) => new(task.Id, task.Title, task.Description, task.Priority, task.Status, task.DueDateUtc!.Value);
}

public sealed record OverdueTaskReportResponse(long ProjectId, int TotalOverdueTaskCount, IReadOnlyList<OverdueTaskMemberResponse> Members);
public sealed record OverdueTaskMemberResponse(long AssignedUserId, string AssignedUserName, int OverdueTaskCount, IReadOnlyList<OverdueTaskResponse> Tasks);
public sealed record OverdueTaskResponse(long Id, string Title, string Description, TaskPriority Priority, TaskState Status, DateTime DueDateUtc);
