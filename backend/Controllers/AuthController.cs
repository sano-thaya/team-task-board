
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using TeamTaskBoard.Api.Services;

namespace TeamTaskBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly FirebaseTokenService _tokenService;

    public AuthController(FirebaseTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpGet("verify")]
    public async Task<IActionResult> VerifyToken()
    {
        // Read the Authorization header.
        var authorizationHeader =
            Request.Headers["Authorization"].ToString();

        // Check whether the Bearer token exists.
        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith(
                "Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized(new
            {
                message = "Authorization token is missing."
            });
        }

        var idToken = authorizationHeader["Bearer ".Length..].Trim();

        if (string.IsNullOrWhiteSpace(idToken))
        {
            return Unauthorized(new
            {
                message = "Authorization token is missing."
            });
        }

        try
        {
            // Ask Firebase to verify the token.
            var decodedToken =
                await _tokenService.VerifyTokenAsync(idToken);

            return Ok(new
            {
                message = "Firebase token is valid.",
                uid = decodedToken.Uid
            });
        }
        catch (FirebaseAuthException)
        {
            return Unauthorized(new
            {
                message = "Firebase token is invalid or expired."
            });
        }
    }
}
