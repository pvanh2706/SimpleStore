using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SimpleStore.Infrastructure.Identity;

public static partial class DevelopmentOwnerSeeder
{
    public static async Task SeedDevelopmentOwnerAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        ILogger logger)
    {
        var email = configuration["DevelopmentOwner:Email"]?.Trim();
        var password = configuration["DevelopmentOwner:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            LogSkipped(logger);
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync(ApplicationRoles.Owner))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(ApplicationRoles.Owner));
            EnsureSucceeded(roleResult, "create the Owner role");
        }

        var owner = await userManager.FindByEmailAsync(email);
        if (owner is null)
        {
            owner = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            var userResult = await userManager.CreateAsync(owner, password);
            EnsureSucceeded(userResult, "create the development Owner");
        }

        if (!await userManager.IsInRoleAsync(owner, ApplicationRoles.Owner))
        {
            var roleResult = await userManager.AddToRoleAsync(owner, ApplicationRoles.Owner);
            EnsureSucceeded(roleResult, "assign the Owner role");
        }

        LogSeeded(logger, email);
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to {action}: {string.Join("; ", result.Errors.Select(error => error.Description))}");
        }
    }

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Information,
        Message = "Development Owner bootstrap skipped because DevelopmentOwner configuration is absent.")]
    private static partial void LogSkipped(ILogger logger);

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Information,
        Message = "Development Owner account is ready for {Email}.")]
    private static partial void LogSeeded(ILogger logger, string email);
}
