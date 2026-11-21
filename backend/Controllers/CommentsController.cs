using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskBoard.Api.Services;

namespace TeamTaskBoard.Api.Controllers;

[ApiController, Authorize, Route("api/projects/{projectId:long}/tasks/{taskId:long}/comments")]
public sealed class CommentsController(ICommentService commentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CommentResponse>>> Get(long projectId, long taskId, CancellationToken cancellationToken)
    {
        var comments = await commentService.GetAsync(projectId, taskId, CurrentUid(), cancellationToken);
        return comments is null ? NotFound(new { message = "Task not found or unavailable." }) : Ok(comments);
    }

    [HttpPost]
    public async Task<ActionResult<CommentResponse>> Create(long projectId, long taskId, CreateCommentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var comment = await commentService.CreateAsync(projectId, taskId, CurrentUid(), request, cancellationToken);
            return comment is null ? NotFound(new { message = "Task not found or unavailable." }) : Ok(comment);
        }
        catch (CommentValidationException exception) { return BadRequest(new { message = exception.Message }); }
    }

    private string CurrentUid() => User.FindFirst("firebase_uid")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new InvalidOperationException("Authenticated user UID is missing.");
}

public sealed class CreateCommentRequest
{
    [Required, MaxLength(2000)] public string Text { get; init; } = string.Empty;
}

public sealed record CommentResponse(string Id, string AuthorName, string Text, DateTime CreatedAtUtc);
