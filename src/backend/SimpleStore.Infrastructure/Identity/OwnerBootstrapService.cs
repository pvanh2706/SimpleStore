using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Infrastructure.Persistence;

namespace SimpleStore.Infrastructure.Identity;

public sealed class OwnerBootstrapService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager)
{
    public async Task<OwnerBootstrapResult> BootstrapAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var trimmedEmail = email?.Trim() ?? string.Empty;
        var normalizedEmail = userManager.NormalizeEmail(trimmedEmail);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return new OwnerBootstrapResult(false, string.Empty, false);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            await ApplicationLock.AcquireOwnerBootstrapAsync(dbContext, cancellationToken);
            if (!await roleManager.RoleExistsAsync(ApplicationRoles.Owner))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(ApplicationRoles.Owner));
                if (!roleResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new OwnerBootstrapResult(false, normalizedEmail, false);
                }
            }

            var owners = await userManager.GetUsersInRoleAsync(ApplicationRoles.Owner);
            if (owners.Count > 0)
            {
                var exact = owners.Count == 1
                    && string.Equals(owners[0].NormalizedEmail, normalizedEmail, StringComparison.Ordinal);
                await transaction.RollbackAsync(cancellationToken);
                return new OwnerBootstrapResult(exact, normalizedEmail, exact);
            }

            if (await dbContext.Users.AnyAsync(
                    item => item.NormalizedEmail == normalizedEmail,
                    cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new OwnerBootstrapResult(false, normalizedEmail, false);
            }

            var owner = new ApplicationUser
            {
                UserName = trimmedEmail.ToLowerInvariant(),
                Email = trimmedEmail.ToLowerInvariant(),
                EmailConfirmed = true
            };
            var createResult = await userManager.CreateAsync(owner, password);
            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new OwnerBootstrapResult(false, normalizedEmail, false);
            }

            var roleAssignment = await userManager.AddToRoleAsync(owner, ApplicationRoles.Owner);
            if (!roleAssignment.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new OwnerBootstrapResult(false, normalizedEmail, false);
            }

            await transaction.CommitAsync(cancellationToken);
            return new OwnerBootstrapResult(true, normalizedEmail, false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public sealed record OwnerBootstrapResult(bool Success, string NormalizedEmail, bool WasAlreadyBootstrapped);
