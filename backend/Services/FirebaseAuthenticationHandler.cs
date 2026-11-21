using System.Security.Claims;
using System.Text.Encodings.Web;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TeamTaskBoard.Api.Data;

namespace TeamTaskBoard.Api.Services;

public sealed class FirebaseAuthenticationOptions : AuthenticationSchemeOptions
{
}

public sealed class FirebaseAuthenticationHandler : AuthenticationHandler<FirebaseAuthenticationOptions>
{
    private readonly FirebaseTokenService _tokenService;
    private readonly AppDbContext _dbContext;

    public FirebaseAuthenticationHandler(IOptionsMonitor<FirebaseAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, FirebaseTokenService tokenService, AppDbContext dbContext)
        : base(options, logger, encoder)
    {
        _tokenService = tokenService;
        _dbContext = dbContext;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = header["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.Fail("Bearer token is missing.");
        }

        try
        {
            var decodedToken = await _tokenService.VerifyTokenAsync(token);
            var email = decodedToken.Claims.TryGetValue("email", out var emailClaim) ? emailClaim?.ToString() : null;
            var user = await _dbContext.Users.AsNoTracking().SingleOrDefaultAsync(item => item.FirebaseUid == decodedToken.Uid, Context.RequestAborted);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, decodedToken.Uid),
                new("firebase_uid", decodedToken.Uid)
            };

            if (!string.IsNullOrWhiteSpace(email)) claims.Add(new Claim(ClaimTypes.Email, email));
            if (user is not null)
            {
                claims.Add(new Claim(ClaimTypes.Name, user.DisplayName));
                claims.Add(new Claim(ClaimTypes.Role, user.Role));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
        }
        catch (FirebaseAuthException exception)
        {
            Logger.LogWarning(exception, "Firebase token verification failed.");
            return AuthenticateResult.Fail("Firebase token is invalid or expired.");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            Logger.LogWarning(exception, "Firebase token authentication failed.");
            return AuthenticateResult.Fail("Firebase token could not be authenticated.");
        }
    }
}
