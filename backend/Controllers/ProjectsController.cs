using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskBoard.Api.Services;

namespace TeamTaskBoard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public sealed class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService) => _projectService = projectService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectResponse>>> GetProjects(CancellationToken cancellationToken)
    {
        return Ok(await _projectService.GetAvailableAsync(CurrentUid(), cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ProjectResponse>> GetProject(long id, CancellationToken cancellationToken)
    {
        var project = await _projectService.GetDetailsAsync(id, CurrentUid(), cancellationToken);
        return project is null ? NotFound(new { message = "Project not found." }) : Ok(project);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> CreateProject(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "Project name is required." });
        try
        {
            var project = await _projectService.CreateAsync(CurrentUid(), request, cancellationToken);
            return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
        }
        catch (ProjectMemberValidationException exception) { return BadRequest(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ProjectResponse>> UpdateProject(long id, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "Project name is required." });
        try
        {
            var project = await _projectService.UpdateAsync(id, CurrentUid(), request, cancellationToken);
            return project is null ? NotFound(new { message = "Project not found or you are not the owner." }) : Ok(project);
        }
        catch (ProjectMemberValidationException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteProject(long id, CancellationToken cancellationToken)
    {
        return await _projectService.DeleteAsync(id, CurrentUid(), cancellationToken)
            ? NoContent()
            : NotFound(new { message = "Project not found or you are not the owner." });
    }

    private string CurrentUid() => User.FindFirst("firebase_uid")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new InvalidOperationException("Authenticated user UID is missing.");
}

/// <summary>Project creation payload. Ownership is taken from the authenticated token.</summary>
public sealed record CreateProjectRequest
{
    [Required, MaxLength(150)] public required string Name { get; init; }
    [MaxLength(2000)] public string? Description { get; init; }
    public IReadOnlyCollection<string> MemberEmails { get; init; } = Array.Empty<string>();
}

/// <summary>Project update payload. Only the owner may update a project.</summary>
public sealed record UpdateProjectRequest
{
    [Required, MaxLength(150)] public required string Name { get; init; }
    [MaxLength(2000)] public string? Description { get; init; }
    public IReadOnlyCollection<string> MemberEmails { get; init; } = Array.Empty<string>();
}

/// <summary>Project member profile returned by the API.</summary>
public sealed record ProjectUserResponse(long Id, string FirebaseUid, string DisplayName, string Email);

/// <summary>Project profile returned by the API.</summary>
public sealed record ProjectResponse(long Id, string Name, string Description, ProjectUserResponse Owner, IReadOnlyList<ProjectUserResponse> Members, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
