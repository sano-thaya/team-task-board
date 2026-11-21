using Microsoft.EntityFrameworkCore;
using TeamTaskBoard.Api.Data;
using TeamTaskBoard.Api.Models;

namespace TeamTaskBoard.Api.Services;

public sealed class UserSyncService : IUserSyncService
{
    private readonly AppDbContext _dbContext;

    public UserSyncService(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<User> SynchronizeAsync(string firebaseUid, string email, string displayName, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(item => item.FirebaseUid == firebaseUid, cancellationToken);
        if (user is null)
        {
            user = new User { FirebaseUid = firebaseUid, Email = email, DisplayName = displayName, Role = "Member", CreatedAtUtc = DateTime.UtcNow };
            _dbContext.Users.Add(user);
        }
        else
        {
            user.Email = email;
            user.DisplayName = displayName;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }
}
