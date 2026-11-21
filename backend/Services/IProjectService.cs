using TeamTaskBoard.Api.Controllers;

namespace TeamTaskBoard.Api.Services;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectResponse>> GetAvailableAsync(string firebaseUid, CancellationToken cancellationToken);
    Task<ProjectResponse?> GetDetailsAsync(long projectId, string firebaseUid, CancellationToken cancellationToken);
    Task<ProjectResponse> CreateAsync(string firebaseUid, CreateProjectRequest request, CancellationToken cancellationToken);
    Task<ProjectResponse?> UpdateAsync(long projectId, string firebaseUid, UpdateProjectRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(long projectId, string firebaseUid, CancellationToken cancellationToken);
}
