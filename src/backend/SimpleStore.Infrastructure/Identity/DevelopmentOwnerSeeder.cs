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

        await SeedDevelopmentCashierAsync(configuration, owner, roleManager, userManager, logger);

        LogSeeded(logger, email);
    }

    private static async Task SeedDevelopmentCashierAsync(
        IConfiguration configuration,
        ApplicationUser owner,
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        var cashierEmail = configuration["DevelopmentCashier:Email"]?.Trim();
        var cashierPassword = configuration["DevelopmentCashier:Password"];
        if (string.IsNullOrWhiteSpace(cashierEmail)
            || string.IsNullOrWhiteSpace(cashierPassword)
            || !owner.StoreId.HasValue)
        {
            return;
        }

        if (!await roleManager.RoleExistsAsync(ApplicationRoles.Cashier))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(ApplicationRoles.Cashier));
            EnsureSucceeded(roleResult, "create the Cashier role");
        }

        var cashier = await userManager.FindByEmailAsync(cashierEmail);
        if (cashier is null)
        {
            cashier = new ApplicationUser
            {
                UserName = cashierEmail,
                Email = cashierEmail,
                EmailConfirmed = true
            };
            cashier.AssignToStore(owner.StoreId.Value);
            EnsureSucceeded(
                await userManager.CreateAsync(cashier, cashierPassword),
                "create the development Cashier");
        }

        if (!await userManager.IsInRoleAsync(cashier, ApplicationRoles.Cashier))
        {
            EnsureSucceeded(
                await userManager.AddToRoleAsync(cashier, ApplicationRoles.Cashier),
                "assign the Cashier role");
        }

        LogCashierSeeded(logger, cashierEmail);
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

    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Information,
        Message = "Development Cashier account is ready for {Email}.")]
    private static partial void LogCashierSeeded(ILogger logger, string email);
}
