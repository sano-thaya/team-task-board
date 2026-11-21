using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskBoard.Api.Services;

namespace TeamTaskBoard.Api.Controllers;

[ApiController, Authorize, Route("api/projects/{projectId:long}/reports")]
public sealed class ReportsController(IOverdueTaskReportService service) : ControllerBase
{
    [HttpGet("overdue-tasks")]
    public async Task<ActionResult<OverdueTaskReportResponse>> GetOverdueTasks(long projectId, CancellationToken cancellationToken)
    {
        var firebaseUid = User.FindFirst("firebase_uid")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(firebaseUid)) return Unauthorized();
        var report = await service.GetAsync(projectId, firebaseUid, cancellationToken);
        return report is null ? NotFound(new { message = "The report was not found or you are not the project owner." }) : Ok(report);
    }
}
