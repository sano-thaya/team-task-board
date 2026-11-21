using Microsoft.EntityFrameworkCore;
using TeamTaskBoard.Api.Controllers;
using TeamTaskBoard.Api.Data;
using TeamTaskBoard.Api.Models;
using TaskState = TeamTaskBoard.Api.Models.TaskStatus;

namespace TeamTaskBoard.Api.Services;

public interface ITaskService
{
    Task<IReadOnlyList<TaskResponse>?> GetAllAsync(long projectId, string uid, string? search, TaskPriority? priority, CancellationToken cancellationToken);
    Task<TaskListResponse?> GetPageAsync(long projectId, string uid, string? search, TaskPriority? priority, TaskState? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<TaskResponse?> GetAsync(long projectId, long taskId, string uid, CancellationToken cancellationToken);
    Task<TaskResponse?> GetByIdAsync(long taskId, string uid, CancellationToken cancellationToken);
    Task<TaskResponse> CreateAsync(long projectId, string uid, CreateTaskRequest request, CancellationToken cancellationToken);
    Task<TaskResponse?> UpdateAsync(long projectId, long taskId, string uid, UpdateTaskRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(long projectId, long taskId, string uid, CancellationToken cancellationToken);
    Task<TaskResponse?> UpdateByIdAsync(long taskId, string uid, UpdateTaskRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteByIdAsync(long taskId, string uid, CancellationToken cancellationToken);
}

public sealed class TaskService : ITaskService
{
    private readonly AppDbContext _db;
    public TaskService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<TaskResponse>?> GetAllAsync(long projectId, string uid, string? search, TaskPriority? priority, CancellationToken ct)
    {
        if (!await HasAccess(projectId, uid, ct)) return null;
        var query = BaseQuery().Where(t => t.ProjectId == projectId);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(t => EF.Functions.Like(t.Title, $"%{search.Trim()}%"));
        if (priority.HasValue) query = query.Where(t => t.Priority == priority.Value);
        return (await query.OrderByDescending(t => t.UpdatedAtUtc).ToListAsync(ct)).Select(Map).ToList();
    }

    public async Task<TaskListResponse?> GetPageAsync(long projectId, string uid, string? search, TaskPriority? priority, TaskState? status, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1 || pageSize is < 1 or > 100) throw new TaskPageValidationException("Page must be at least 1 and pageSize must be between 1 and 100.");
        if (!await HasAccess(projectId, uid, ct)) return null;
        var query = BaseQuery().Where(t => t.ProjectId == projectId);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(t => EF.Functions.Like(t.Title, $"%{search.Trim()}%"));
        if (priority.HasValue) query = query.Where(t => t.Priority == priority.Value);
        if (status.HasValue) query = query.Where(t => t.Status == status.Value);
        var totalCount = await query.CountAsync(ct);
        var items = (await query.OrderByDescending(t => t.UpdatedAtUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct)).Select(Map).ToList();
        return new TaskListResponse(items, page, pageSize, totalCount, (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task<TaskResponse?> GetAsync(long projectId, long taskId, string uid, CancellationToken ct)
    {
        if (!await HasAccess(projectId, uid, ct)) return null;
        var task = await BaseQuery().SingleOrDefaultAsync(t => t.ProjectId == projectId && t.Id == taskId, ct);
        return task is null ? null : Map(task);
    }

    public async Task<TaskResponse?> GetByIdAsync(long taskId, string uid, CancellationToken ct)
    {
        var projectId = await _db.Tasks.Where(task => task.Id == taskId).Select(task => (long?)task.ProjectId).SingleOrDefaultAsync(ct);
        return projectId.HasValue ? await GetAsync(projectId.Value, taskId, uid, ct) : null;
    }

    public async Task<TaskResponse> CreateAsync(long projectId, string uid, CreateTaskRequest request, CancellationToken ct)
    {
        await ValidateInput(request.Title, request.Description, request.Priority, request.Status, request.DueDateUtc);
        if (!await HasAccess(projectId, uid, ct)) throw new TaskNotFoundException();
        if (!await IsParticipant(projectId, request.AssignedUserId, ct)) throw new TaskValidationException("The assigned user is not a member of this project.");
        var now = DateTime.UtcNow;
        var task = new TaskItem { ProjectId = projectId, Title = request.Title.Trim(), Description = request.Description?.Trim() ?? "", AssignedUserId = request.AssignedUserId, Priority = request.Priority, Status = request.Status, DueDateUtc = request.DueDateUtc, CreatedAtUtc = now, UpdatedAtUtc = now };
        _db.Tasks.Add(task); await _db.SaveChangesAsync(ct);
        return (await GetAsync(projectId, task.Id, uid, ct))!;
    }

    public async Task<TaskResponse?> UpdateAsync(long projectId, long taskId, string uid, UpdateTaskRequest request, CancellationToken ct)
    {
        await ValidateInput(request.Title, request.Description, request.Priority, request.Status, request.DueDateUtc);
        if (!await HasAccess(projectId, uid, ct)) return null;
        var task = await _db.Tasks.SingleOrDefaultAsync(t => t.ProjectId == projectId && t.Id == taskId, ct);
        if (task is null) return null;
        if (!await IsParticipant(projectId, request.AssignedUserId, ct)) throw new TaskValidationException("The assigned user is not a member of this project.");
        task.Title = request.Title.Trim(); task.Description = request.Description?.Trim() ?? ""; task.AssignedUserId = request.AssignedUserId; task.Priority = request.Priority; task.Status = request.Status; task.DueDateUtc = request.DueDateUtc; task.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct); return (await GetAsync(projectId, taskId, uid, ct))!;
    }

    public async Task<bool> DeleteAsync(long projectId, long taskId, string uid, CancellationToken ct)
    {
        if (!await HasAccess(projectId, uid, ct)) return false;
        var task = await _db.Tasks.SingleOrDefaultAsync(t => t.ProjectId == projectId && t.Id == taskId, ct);
        if (task is null) return false;
        _db.Tasks.Remove(task); await _db.SaveChangesAsync(ct); return true;
    }

    public async Task<TaskResponse?> UpdateByIdAsync(long taskId, string uid, UpdateTaskRequest request, CancellationToken ct)
    {
        var projectId = await _db.Tasks.Where(task => task.Id == taskId).Select(task => (long?)task.ProjectId).SingleOrDefaultAsync(ct);
        return projectId.HasValue ? await UpdateAsync(projectId.Value, taskId, uid, request, ct) : null;
    }

    public async Task<bool> DeleteByIdAsync(long taskId, string uid, CancellationToken ct)
    {
        var projectId = await _db.Tasks.Where(task => task.Id == taskId).Select(task => (long?)task.ProjectId).SingleOrDefaultAsync(ct);
        return projectId.HasValue && await DeleteAsync(projectId.Value, taskId, uid, ct);
    }

    private IQueryable<TaskItem> BaseQuery() => _db.Tasks.AsNoTracking().Include(t => t.AssignedUser);
    private Task<bool> HasAccess(long projectId, string uid, CancellationToken ct) => _db.Projects.AnyAsync(p => p.Id == projectId && (p.Owner.FirebaseUid == uid || p.Members.Any(m => m.User.FirebaseUid == uid)), ct);
    private Task<bool> IsParticipant(long projectId, long userId, CancellationToken ct) => _db.Projects.AnyAsync(p => p.Id == projectId && (p.OwnerId == userId || p.Members.Any(m => m.UserId == userId)), ct);
    private static Task ValidateInput(string title, string? description, TaskPriority priority, TaskState status, DateTime? dueDateUtc)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 150) throw new TaskValidationException("Task title is required and must be 150 characters or fewer.");
        if ((description?.Length ?? 0) > 2000) throw new TaskValidationException("Task description must be 2000 characters or fewer.");
        if (!Enum.IsDefined(priority) || !Enum.IsDefined(status)) throw new TaskValidationException("Invalid task priority or status.");
        if (dueDateUtc.HasValue && dueDateUtc.Value.Date < DateTime.UtcNow.Date) throw new TaskValidationException("Due date cannot be in the past.");
        return Task.CompletedTask;
    }
    private static TaskResponse Map(TaskItem t) => new(t.Id, t.ProjectId, t.Title, t.Description, new ProjectUserResponse(t.AssignedUser.Id, t.AssignedUser.FirebaseUid, t.AssignedUser.DisplayName, t.AssignedUser.Email), t.Priority, t.Status, t.DueDateUtc, t.CreatedAtUtc, t.UpdatedAtUtc);
}

public sealed class TaskValidationException(string message) : Exception(message);
public sealed class TaskNotFoundException : Exception;
public sealed class TaskPageValidationException(string message) : Exception(message);
