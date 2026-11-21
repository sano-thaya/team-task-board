using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskBoard.Api.Models;
using TeamTaskBoard.Api.Services;
using TaskState = TeamTaskBoard.Api.Models.TaskStatus;

namespace TeamTaskBoard.Api.Controllers;

[ApiController, Authorize, Route("api/projects/{projectId:long}/tasks")]
public class TasksController(ITaskService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TaskListResponse>> GetAll(long projectId, [FromQuery] string? search, [FromQuery] TaskPriority? priority, [FromQuery] TaskState? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        try { var result = await service.GetPageAsync(projectId, CurrentUid(), search, priority, status, page, pageSize, ct); return result is null ? NotFound() : Ok(result); }
        catch (TaskPageValidationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{taskId:long}")]
    public async Task<ActionResult<TaskResponse>> Get(long projectId, long taskId, CancellationToken ct)
    {
        var result = await service.GetAsync(projectId, taskId, CurrentUid(), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<TaskResponse>> Create(long projectId, CreateTaskRequest request, CancellationToken ct)
    {
        try { var result = await service.CreateAsync(projectId, CurrentUid(), request, ct); return CreatedAtAction(nameof(Get), new { projectId, taskId = result.Id }, result); }
        catch (TaskNotFoundException) { return NotFound(); }
        catch (TaskValidationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{taskId:long}")]
    public async Task<ActionResult<TaskResponse>> Update(long projectId, long taskId, UpdateTaskRequest request, CancellationToken ct)
    {
        try { var result = await service.UpdateAsync(projectId, taskId, CurrentUid(), request, ct); return result is null ? NotFound() : Ok(result); }
        catch (TaskValidationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{taskId:long}")]
    public async Task<IActionResult> Delete(long projectId, long taskId, CancellationToken ct) => await service.DeleteAsync(projectId, taskId, CurrentUid(), ct) ? NoContent() : NotFound();
    private string CurrentUid() => User.FindFirst("firebase_uid")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
}

public sealed class CreateTaskRequest
{
    [Required, MaxLength(150)] public string Title { get; init; } = string.Empty;
    [MaxLength(2000)] public string? Description { get; init; }
    public long AssignedUserId { get; init; }
    public TaskPriority Priority { get; init; }
    public TaskState Status { get; init; } = TaskState.ToDo;
    public DateTime? DueDateUtc { get; init; }
}

public sealed class UpdateTaskRequest
{
    [Required, MaxLength(150)] public string Title { get; init; } = string.Empty;
    [MaxLength(2000)] public string? Description { get; init; }
    public long AssignedUserId { get; init; }
    public TaskPriority Priority { get; init; }
    public TaskState Status { get; init; }
    public DateTime? DueDateUtc { get; init; }
}
public record TaskResponse(long Id, long ProjectId, string Title, string Description, ProjectUserResponse AssignedUser, TaskPriority Priority, TaskState Status, DateTime? DueDateUtc, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public record TaskListResponse(IReadOnlyList<TaskResponse> Items, int Page, int PageSize, int TotalCount, int TotalPages);
