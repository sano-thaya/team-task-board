using Microsoft.EntityFrameworkCore;
using TeamTaskBoard.Api.Controllers;
using TeamTaskBoard.Api.Data;
using TeamTaskBoard.Api.Models;

namespace TeamTaskBoard.Api.Services;

public sealed class ProjectService : IProjectService
{
    private readonly AppDbContext _dbContext;

    public ProjectService(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<ProjectResponse>> GetAvailableAsync(string firebaseUid, CancellationToken cancellationToken)
    {
        var projects = await BaseQuery()
            .Where(project => project.Owner.FirebaseUid == firebaseUid || project.Members.Any(member => member.User.FirebaseUid == firebaseUid))
            .OrderByDescending(project => project.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
        return projects.Select(Map).ToList();
    }

    public async Task<ProjectResponse?> GetDetailsAsync(long projectId, string firebaseUid, CancellationToken cancellationToken)
    {
        var project = await BaseQuery().SingleOrDefaultAsync(project => project.Id == projectId &&
            (project.Owner.FirebaseUid == firebaseUid || project.Members.Any(member => member.User.FirebaseUid == firebaseUid)), cancellationToken);
        return project is null ? null : Map(project);
    }

    public async Task<ProjectResponse> CreateAsync(string firebaseUid, CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var owner = await _dbContext.Users.SingleOrDefaultAsync(user => user.FirebaseUid == firebaseUid, cancellationToken)
            ?? throw new InvalidOperationException("The authenticated user has not been synchronized.");
        var memberUsers = await GetMembersAsync(request.MemberEmails, owner.Id, cancellationToken);
        var now = DateTime.UtcNow;
        var project = new Project
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            OwnerId = owner.Id,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Members = memberUsers.Select(user => new ProjectMember { User = user }).ToList()
        };
        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetDetailsAsync(project.Id, firebaseUid, cancellationToken))!;
    }

    public async Task<ProjectResponse?> UpdateAsync(long projectId, string firebaseUid, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await _dbContext.Projects
            .Include(item => item.Owner)
            .Include(item => item.Members).ThenInclude(member => member.User)
            .SingleOrDefaultAsync(item => item.Id == projectId && item.Owner.FirebaseUid == firebaseUid, cancellationToken);
        if (project is null) return null;

        var memberUsers = await GetMembersAsync(request.MemberEmails, project.OwnerId, cancellationToken);
        project.Name = request.Name.Trim();
        project.Description = request.Description?.Trim() ?? string.Empty;
        project.UpdatedAtUtc = DateTime.UtcNow;
        project.Members.Clear();
        foreach (var user in memberUsers) project.Members.Add(new ProjectMember { ProjectId = project.Id, UserId = user.Id, User = user });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetDetailsAsync(project.Id, firebaseUid, cancellationToken))!;
    }

    public async Task<bool> DeleteAsync(long projectId, string firebaseUid, CancellationToken cancellationToken)
    {
        var project = await _dbContext.Projects.Include(item => item.Owner)
            .SingleOrDefaultAsync(item => item.Id == projectId && item.Owner.FirebaseUid == firebaseUid, cancellationToken);
        if (project is null) return false;
        _dbContext.Projects.Remove(project);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<Project> BaseQuery() => _dbContext.Projects.AsNoTracking()
        .Include(project => project.Owner)
        .Include(project => project.Members).ThenInclude(member => member.User);

    private async Task<List<User>> GetMembersAsync(IEnumerable<string> memberEmails, long ownerId, CancellationToken cancellationToken)
    {
        var requestedEmails = memberEmails.Where(email => !string.IsNullOrWhiteSpace(email)).Select(email => email.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var users = await _dbContext.Users.Where(user => requestedEmails.Contains(user.Email) && user.Id != ownerId).ToListAsync(cancellationToken);
        if (users.Count != requestedEmails.Length) throw new ProjectMemberValidationException("One or more project member emails do not belong to registered users.");
        return users;
    }

    private static ProjectResponse Map(Project project) => new(
        project.Id,
        project.Name,
        project.Description,
        new ProjectUserResponse(project.Owner.FirebaseUid, project.Owner.DisplayName, project.Owner.Email),
        project.Members.Select(member => new ProjectUserResponse(member.User.FirebaseUid, member.User.DisplayName, member.User.Email)).ToList(),
        project.CreatedAtUtc,
        project.UpdatedAtUtc);
}

public sealed class ProjectMemberValidationException : Exception
{
    public ProjectMemberValidationException(string message) : base(message) { }
}
