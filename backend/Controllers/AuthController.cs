using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TeamTaskBoard.Api.Services;

namespace TeamTaskBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly FirebaseTokenService _tokenService;
    private readonly IUserSyncService _userSyncService;

    public AuthController(FirebaseTokenService tokenService, IUserSyncService userSyncService)
    {
        _tokenService = tokenService;
        _userSyncService = userSyncService;
    }

    [HttpGet("verify")]
    public async Task<IActionResult> VerifyToken(CancellationToken cancellationToken)
    {
        var idToken = GetBearerToken();
        if (idToken is null) return Unauthorized(new { message = "Authorization token is missing." });

        try
        {
            var decodedToken = await _tokenService.VerifyTokenAsync(idToken);
            return Ok(new { message = "Firebase token is valid.", uid = decodedToken.Uid });
        }
        catch (FirebaseAuthException)
        {
            return Unauthorized(new { message = "Firebase token is invalid or expired." });
        }
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncUser([FromBody] SyncUserRequest request, CancellationToken cancellationToken)
    {
        var idToken = GetBearerToken();
        if (idToken is null) return Unauthorized(new { message = "Authorization token is missing." });

        var displayName = request.DisplayName.Trim();
        if (displayName.Length == 0) return BadRequest(new { message = "Display name is required." });

        try
        {
            var decodedToken = await _tokenService.VerifyTokenAsync(idToken);
            var email = decodedToken.Claims.TryGetValue("email", out var emailClaim) ? emailClaim?.ToString() : null;
            if (string.IsNullOrWhiteSpace(email)) return Unauthorized(new { message = "Email not found in Firebase token." });

            var user = await _userSyncService.SynchronizeAsync(decodedToken.Uid, email, displayName, cancellationToken);
            return Ok(new
            {
                message = "User synchronized successfully.",
                user = new UserResponse(user.Id, user.FirebaseUid, user.Email, user.DisplayName, user.Role)
            });
        }
        catch (FirebaseAuthException)
        {
            return Unauthorized(new { message = "Firebase token is invalid or expired." });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            uid = User.FindFirst("firebase_uid")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
            displayName = User.Identity?.Name,
            role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Member"
        });
    }

    private string? GetBearerToken()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;
        var token = header["Bearer ".Length..].Trim();
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }
}

/// <summary>Payload used to synchronize the Firebase user's profile.</summary>
public sealed record SyncUserRequest
{
    [Required, MaxLength(100)]
    public required string DisplayName { get; init; }
}

/// <summary>Profile returned after a user is synchronized.</summary>
public sealed record UserResponse(long Id, string FirebaseUid, string Email, string DisplayName, string Role);
