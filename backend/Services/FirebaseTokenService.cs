
using FirebaseAdmin.Auth;

namespace TeamTaskBoard.Api.Services;

public class FirebaseTokenService
{
    private readonly FirebaseAuth _firebaseAuth;

    public FirebaseTokenService(FirebaseAuth firebaseAuth)
    {
        _firebaseAuth = firebaseAuth;
    }

    public async Task<FirebaseToken> VerifyTokenAsync(string idToken)
    {
        return await _firebaseAuth.VerifyIdTokenAsync(idToken);
    }
}
