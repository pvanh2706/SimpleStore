using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return InvalidCredentials();
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return InvalidCredentials();
        }

        return Ok(await CreateSessionResponseAsync(user));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("session")]
    public async Task<ActionResult<SessionResponse>> Session(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(new SessionResponse(false, null, null, [], false));
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Ok(new SessionResponse(false, null, null, [], false));
        }

        return Ok(await CreateSessionResponseAsync(user));
    }

    private async Task<SessionResponse> CreateSessionResponseAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new SessionResponse(
            true,
            user.Email,
            user.StoreId,
            roles.ToArray(),
            user.StoreId.HasValue);
    }

    private ObjectResult InvalidCredentials() => Problem(
        statusCode: StatusCodes.Status401Unauthorized,
        title: "Email hoặc mật khẩu không đúng.",
        type: "https://simplestore/errors/invalid-credentials",
        extensions: new Dictionary<string, object?>
        {
            ["code"] = "invalid-credentials"
        });
}

public sealed record LoginRequest(string Email, string Password, bool RememberMe = false);

public sealed record SessionResponse(
    bool IsAuthenticated,
    string? Email,
    Guid? StoreId,
    IReadOnlyCollection<string> Roles,
    bool HasStore);
