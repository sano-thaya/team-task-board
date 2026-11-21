using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskBoard.Api.Services;

namespace TeamTaskBoard.Api.Controllers;

[ApiController, Authorize, Route("api/tasks")]
public sealed class TaskCompatibilityController(ITaskService service) : ControllerBase
{
    [HttpGet("{taskId:long}")]
    public async Task<ActionResult<TaskResponse>> Get(long taskId, CancellationToken ct) => Result(await service.GetByIdAsync(taskId, CurrentUid(), ct));

    [HttpPut("{taskId:long}")]
    public async Task<ActionResult<TaskResponse>> Update(long taskId, UpdateTaskRequest request, CancellationToken ct)
    {
        try { return Result(await service.UpdateByIdAsync(taskId, CurrentUid(), request, ct)); }
        catch (TaskValidationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{taskId:long}")]
    public async Task<IActionResult> Delete(long taskId, CancellationToken ct) => await service.DeleteByIdAsync(taskId, CurrentUid(), ct) ? NoContent() : NotFound();

    private ActionResult<TaskResponse> Result(TaskResponse? response) => response is null ? NotFound() : Ok(response);
    private string CurrentUid() => User.FindFirst("firebase_uid")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
}
