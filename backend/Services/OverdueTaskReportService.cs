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
        var rows = db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true
            ? await GetInMemoryRowsAsync(projectId, firebaseUid, cancellationToken)
            : await db.OverdueTaskReportRows.FromSqlInterpolated($"""
                SELECT t.Id AS TaskId, t.Title, t.Description, t.AssignedUserId,
                       assigned.DisplayName AS AssignedUserName, t.Priority, t.Status,
                       t.DueDateUtc,
                       COUNT(*) OVER (PARTITION BY t.AssignedUserId) AS OverdueTaskCount
                FROM Tasks t
                INNER JOIN Projects p ON p.Id = t.ProjectId
                INNER JOIN Users owner ON owner.Id = p.OwnerId
                INNER JOIN Users assigned ON assigned.Id = t.AssignedUserId
                WHERE p.Id = {projectId}
                  AND owner.FirebaseUid = {firebaseUid}
                  AND t.DueDateUtc IS NOT NULL
                  AND t.DueDateUtc < UTC_TIMESTAMP(6)
                  AND t.Status <> {(int)TaskState.Completed}
                GROUP BY t.Id, t.Title, t.Description, t.AssignedUserId,
                         assigned.DisplayName, t.Priority, t.Status, t.DueDateUtc
                ORDER BY assigned.DisplayName, t.DueDateUtc, t.Id
                """).AsNoTracking().ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            var projectExists = await db.Projects.AsNoTracking().AnyAsync(project => project.Id == projectId, cancellationToken);
            return projectExists && await db.Projects.AsNoTracking().AnyAsync(project => project.Id == projectId && project.Owner.FirebaseUid == firebaseUid, cancellationToken)
                ? new OverdueTaskReportResponse(projectId, 0, Array.Empty<OverdueTaskMemberResponse>())
                : null;
        }

        var members = rows.GroupBy(row => new { row.AssignedUserId, row.AssignedUserName })
            .Select(group => new OverdueTaskMemberResponse(group.Key.AssignedUserId, group.Key.AssignedUserName, group.First().OverdueTaskCount > 0 ? (int)group.First().OverdueTaskCount : group.Count(), group.Select(Map).ToList()))
            .ToList();
        return new OverdueTaskReportResponse(projectId, rows.Count, members);
    }

    private async Task<List<OverdueTaskReportRow>> GetInMemoryRowsAsync(long projectId, string firebaseUid, CancellationToken cancellationToken)
    {
        var isOwner = await db.Projects.AsNoTracking().AnyAsync(project => project.Id == projectId && project.Owner.FirebaseUid == firebaseUid, cancellationToken);
        if (!isOwner) return [];
        var nowUtc = DateTime.UtcNow;
        var tasks = await db.Tasks.AsNoTracking().Include(task => task.AssignedUser).Where(task => task.ProjectId == projectId && task.DueDateUtc.HasValue && task.DueDateUtc.Value < nowUtc && task.Status != TaskState.Completed).OrderBy(task => task.AssignedUser.DisplayName).ThenBy(task => task.DueDateUtc).ThenBy(task => task.Id).ToListAsync(cancellationToken);
        return tasks.GroupBy(task => task.AssignedUserId).SelectMany(group => group.Select(task => new OverdueTaskReportRow { TaskId = task.Id, Title = task.Title, Description = task.Description, AssignedUserId = task.AssignedUserId, AssignedUserName = task.AssignedUser.DisplayName, Priority = (int)task.Priority, Status = (int)task.Status, DueDateUtc = task.DueDateUtc!.Value, OverdueTaskCount = group.LongCount() })).ToList();
    }

    private static OverdueTaskResponse Map(OverdueTaskReportRow row) => new(row.TaskId, row.Title, row.Description, (TaskPriority)row.Priority, (TaskState)row.Status, row.DueDateUtc);
}

public sealed record OverdueTaskReportResponse(long ProjectId, int TotalOverdueTaskCount, IReadOnlyList<OverdueTaskMemberResponse> Members);
public sealed record OverdueTaskMemberResponse(long AssignedUserId, string AssignedUserName, int OverdueTaskCount, IReadOnlyList<OverdueTaskResponse> Tasks);
public sealed record OverdueTaskResponse(long Id, string Title, string Description, TaskPriority Priority, TaskState Status, DateTime DueDateUtc);
