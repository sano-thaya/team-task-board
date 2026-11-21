using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore;
using TeamTaskBoard.Api.Controllers;
using TeamTaskBoard.Api.Data;

namespace TeamTaskBoard.Api.Services;

public interface ICommentService
{
    Task<IReadOnlyList<CommentResponse>?> GetAsync(long projectId, long taskId, string firebaseUid, CancellationToken cancellationToken);
    Task<CommentResponse?> CreateAsync(long projectId, long taskId, string firebaseUid, CreateCommentRequest request, CancellationToken cancellationToken);
}

public sealed class CommentService : ICommentService
{
    private readonly AppDbContext _db;
    private readonly FirestoreDb _firestore;

    public CommentService(AppDbContext db, FirestoreDb firestore)
    {
        _db = db;
        _firestore = firestore;
    }

    public async Task<IReadOnlyList<CommentResponse>?> GetAsync(long projectId, long taskId, string firebaseUid, CancellationToken cancellationToken)
    {
        if (!await HasTaskAccessAsync(projectId, taskId, firebaseUid, cancellationToken)) return null;
        var snapshot = await CommentsCollection(projectId, taskId).OrderBy("createdAtUtc").GetSnapshotAsync(cancellationToken);
        return snapshot.Documents.Select(Map).ToList();
    }

    public async Task<CommentResponse?> CreateAsync(long projectId, long taskId, string firebaseUid, CreateCommentRequest request, CancellationToken cancellationToken)
    {
        if (!await HasTaskAccessAsync(projectId, taskId, firebaseUid, cancellationToken)) return null;
        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Trim().Length > 2000)
            throw new CommentValidationException("Comment text is required and must be 2000 characters or fewer.");

        var author = await _db.Users.SingleOrDefaultAsync(user => user.FirebaseUid == firebaseUid, cancellationToken);
        if (author is null) return null;

        var createdAtUtc = DateTime.UtcNow;
        DocumentSnapshot? parent = null;
        if (!string.IsNullOrWhiteSpace(request.ParentCommentId))
        {
            parent = await CommentsCollection(projectId, taskId).Document(request.ParentCommentId).GetSnapshotAsync(cancellationToken);
            if (!parent.Exists) throw new CommentValidationException("The comment being replied to was not found.");
            if (parent.ContainsField("parentCommentId")) throw new CommentValidationException("Replies can only be added to top-level comments.");
        }

        var data = new Dictionary<string, object>
        {
            ["projectId"] = projectId,
            ["taskId"] = taskId,
            ["authorUid"] = author.FirebaseUid,
            ["authorName"] = author.DisplayName,
            ["text"] = request.Text.Trim(),
            ["createdAtUtc"] = Timestamp.FromDateTime(createdAtUtc)
        };
        if (parent is not null) data["parentCommentId"] = parent.Id;
        var comment = await CommentsCollection(projectId, taskId).AddAsync(data, cancellationToken);
        await _firestore.Collection("commentEvents").Document(EventId(projectId, taskId)).SetAsync(
            new Dictionary<string, object> { ["lastCommentAtUtc"] = Timestamp.FromDateTime(createdAtUtc) },
            cancellationToken: cancellationToken);
        return new CommentResponse(comment.Id, author.DisplayName, request.Text.Trim(), createdAtUtc, parent?.Id);
    }

    private CollectionReference CommentsCollection(long projectId, long taskId) => _firestore
        .Collection("projects").Document(projectId.ToString()).Collection("tasks").Document(taskId.ToString()).Collection("comments");

    private Task<bool> HasTaskAccessAsync(long projectId, long taskId, string firebaseUid, CancellationToken cancellationToken) =>
        _db.Tasks.AnyAsync(task => task.ProjectId == projectId && task.Id == taskId &&
            (task.Project.Owner.FirebaseUid == firebaseUid || task.Project.Members.Any(member => member.User.FirebaseUid == firebaseUid)), cancellationToken);

    private static string EventId(long projectId, long taskId) => $"{projectId}_{taskId}";

    private static CommentResponse Map(DocumentSnapshot document)
    {
        var timestamp = document.GetValue<Timestamp>("createdAtUtc").ToDateTime();
        var parentCommentId = document.ContainsField("parentCommentId") ? document.GetValue<string>("parentCommentId") : null;
        return new CommentResponse(document.Id, document.GetValue<string>("authorName"), document.GetValue<string>("text"), timestamp, parentCommentId);
    }
}

public sealed class CommentValidationException(string message) : Exception(message);
