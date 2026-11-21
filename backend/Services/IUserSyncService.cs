using TeamTaskBoard.Api.Models;

namespace TeamTaskBoard.Api.Services;

public interface IUserSyncService
{
    Task<User> SynchronizeAsync(string firebaseUid, string email, string displayName, CancellationToken cancellationToken);
}
