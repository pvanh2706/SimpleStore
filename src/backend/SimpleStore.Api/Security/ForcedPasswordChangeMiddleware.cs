using Microsoft.AspNetCore.Identity;
using SimpleStore.Api.Operations;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Api.Security;

public sealed class ForcedPasswordChangeMiddleware(RequestDelegate next)
{
    private static readonly HashSet<PathString> AllowedApiPaths =
    [
        new("/api/auth/session"),
        new("/api/auth/change-password"),
        new("/api/auth/logout"),
        new("/api/security/antiforgery")
    ];

    public async Task InvokeAsync(
        HttpContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user is null || !user.IsEnabled)
            {
                await signInManager.SignOutAsync();
                if (context.Request.Path != "/api/auth/session")
                {
                    await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "invalid-session", "Authentication is required.");
                    return;
                }
            }
            else if (user.MustChangePassword
                && context.Request.Path.StartsWithSegments("/api")
                && !AllowedApiPaths.Contains(context.Request.Path))
            {
                await WriteProblemAsync(
                    context,
                    StatusCodes.Status403Forbidden,
                    "password-change-required",
                    "Change the temporary password before using business features.");
                return;
            }

            if (user is not null)
            {
                context.Items[OperationalContext.UserIdItem] = user.Id;
                context.Items[OperationalContext.StoreIdItem] = user.StoreId;
            }
        }

        await next(context);
    }

    private static Task WriteProblemAsync(HttpContext context, int status, string code, string title) =>
        Results.Problem(
            statusCode: status,
            title: title,
            type: $"https://simplestore/errors/{code}",
            extensions: new Dictionary<string, object?> { ["code"] = code })
        .ExecuteAsync(context);
}
