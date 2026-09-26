using System.Data;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Application.Errors;
using SimpleStore.Infrastructure.Persistence;

namespace SimpleStore.Infrastructure.Identity;

public sealed class AccountManagementService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<CashierAccountResult>> ListCashiersAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken)
    {
        var owner = await GetOwnerAsync(ownerUserId, cancellationToken);
        var cashierRole = await roleManager.FindByNameAsync(ApplicationRoles.Cashier);
        if (cashierRole is null)
        {
            return [];
        }

        return await (
                from user in dbContext.Users.AsNoTracking()
                join userRole in dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                where user.StoreId == owner.StoreId && userRole.RoleId == cashierRole.Id
                orderby user.NormalizedEmail, user.Id
                select new CashierAccountResult(
                    user.Id,
                    user.Email!,
                    user.IsEnabled,
                    user.MustChangePassword,
                    user.DisabledAt,
                    user.PasswordChangeRequiredAt,
                    user.PasswordChangedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<CashierCredentialResult> CreateCashierAsync(
        Guid ownerUserId,
        string email,
        CancellationToken cancellationToken)
    {
        var owner = await GetOwnerAsync(ownerUserId, cancellationToken);
        var normalizedEmail = NormalizeEmail(email);
        var temporaryPassword = TemporaryPasswordGenerator.Create();
        var now = timeProvider.GetUtcNow();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            if (await userManager.FindByEmailAsync(normalizedEmail) is not null)
            {
                throw new ApplicationConflictException("duplicate-email", "An account with this email already exists.");
            }

            await EnsureCashierRoleAsync();
            var cashier = new ApplicationUser
            {
                UserName = normalizedEmail,
                Email = normalizedEmail,
                EmailConfirmed = true
            };
            cashier.AssignToStore(owner.StoreId!.Value);
            cashier.RequirePasswordChange(now);
            EnsureSucceeded(await userManager.CreateAsync(cashier, temporaryPassword), "cashier-credential-invalid");
            EnsureSucceeded(await userManager.AddToRoleAsync(cashier, ApplicationRoles.Cashier), "cashier-role-failed");
            dbContext.AccountLifecycleAudits.Add(AccountLifecycleAudit.Create(
                owner.StoreId.Value,
                cashier.Id,
                AccountLifecycleAction.CashierCreated,
                owner.Id,
                now));
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new CashierCredentialResult(cashier.Id, cashier.Email!, temporaryPassword, true, false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<CashierAccountResult> DisableCashierAsync(
        Guid ownerUserId,
        Guid cashierId,
        CancellationToken cancellationToken)
    {
        var owner = await GetOwnerAsync(ownerUserId, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            var cashier = await GetCashierAsync(owner.StoreId!.Value, cashierId, cancellationToken);
            if (cashier.IsEnabled)
            {
                var now = timeProvider.GetUtcNow();
                cashier.Disable(now);
                EnsureSucceeded(await userManager.UpdateSecurityStampAsync(cashier), "cashier-disable-failed");
                dbContext.AccountLifecycleAudits.Add(AccountLifecycleAudit.Create(
                    owner.StoreId.Value,
                    cashier.Id,
                    AccountLifecycleAction.CashierDisabled,
                    owner.Id,
                    now));
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return ToResult(cashier);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<CashierCredentialResult> ResetCredentialAsync(
        Guid ownerUserId,
        Guid cashierId,
        CancellationToken cancellationToken)
    {
        var owner = await GetOwnerAsync(ownerUserId, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            var cashier = await GetCashierAsync(owner.StoreId!.Value, cashierId, cancellationToken);
            if (!cashier.IsEnabled)
            {
                throw new ApplicationConflictException("cashier-disabled", "Disabled Cashier credentials cannot be reset.");
            }

            var temporaryPassword = TemporaryPasswordGenerator.Create();
            var token = await userManager.GeneratePasswordResetTokenAsync(cashier);
            EnsureSucceeded(
                await userManager.ResetPasswordAsync(cashier, token, temporaryPassword),
                "cashier-credential-invalid");
            var now = timeProvider.GetUtcNow();
            cashier.RequirePasswordChange(now);
            EnsureSucceeded(await userManager.UpdateAsync(cashier), "cashier-reset-failed");
            dbContext.AccountLifecycleAudits.Add(AccountLifecycleAudit.Create(
                owner.StoreId.Value,
                cashier.Id,
                AccountLifecycleAction.CredentialReset,
                owner.Id,
                now));
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new CashierCredentialResult(cashier.Id, cashier.Email!, temporaryPassword, true, false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            var user = await userManager.FindByIdAsync(userId.ToString())
                ?? throw new ApplicationNotFoundException("user-not-found", "User was not found.");
            if (!user.IsEnabled)
            {
                throw new ApplicationConflictException("account-disabled", "The account is disabled.");
            }

            EnsureSucceeded(
                await userManager.ChangePasswordAsync(user, currentPassword, newPassword),
                "password-change-failed");
            var now = timeProvider.GetUtcNow();
            user.CompletePasswordChange(now);
            EnsureSucceeded(await userManager.UpdateAsync(user), "password-change-failed");
            if (user.StoreId.HasValue)
            {
                dbContext.AccountLifecycleAudits.Add(AccountLifecycleAudit.Create(
                    user.StoreId.Value,
                    user.Id,
                    AccountLifecycleAction.CredentialChanged,
                    user.Id,
                    now));
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<ApplicationUser> GetOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        var owner = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == ownerUserId, cancellationToken)
            ?? throw new ApplicationNotFoundException("owner-not-found", "Owner was not found.");
        if (!owner.IsEnabled || !owner.StoreId.HasValue || !await userManager.IsInRoleAsync(owner, ApplicationRoles.Owner))
        {
            throw new ApplicationConflictException("owner-store-required", "An enabled Store Owner is required.");
        }

        return owner;
    }

    private async Task<ApplicationUser> GetCashierAsync(
        Guid storeId,
        Guid cashierId,
        CancellationToken cancellationToken)
    {
        var cashier = await dbContext.Users.SingleOrDefaultAsync(
            item => item.Id == cashierId && item.StoreId == storeId,
            cancellationToken);
        if (cashier is null || !await userManager.IsInRoleAsync(cashier, ApplicationRoles.Cashier))
        {
            throw new ApplicationNotFoundException("cashier-not-found", "Cashier was not found.");
        }

        return cashier;
    }

    private async Task EnsureCashierRoleAsync()
    {
        if (!await roleManager.RoleExistsAsync(ApplicationRoles.Cashier))
        {
            EnsureSucceeded(
                await roleManager.CreateAsync(new IdentityRole<Guid>(ApplicationRoles.Cashier)),
                "cashier-role-failed");
        }
    }

    private string NormalizeEmail(string email)
    {
        var trimmed = email?.Trim() ?? string.Empty;
        var normalized = userManager.NormalizeEmail(trimmed);
        if (string.IsNullOrWhiteSpace(trimmed) || string.IsNullOrWhiteSpace(normalized))
        {
            throw new ApplicationValidationException(
                "email-required",
                "Email is required.",
                [new ValidationError(null, "email", "email-required", "Email is required.")]);
        }

        return trimmed.ToLowerInvariant();
    }

    private static void EnsureSucceeded(IdentityResult result, string code)
    {
        if (!result.Succeeded)
        {
            throw new ApplicationValidationException(
                code,
                "The credential does not satisfy the account policy.",
                result.Errors.Select(error => new ValidationError(null, "credential", error.Code, error.Description)).ToArray());
        }
    }

    private static CashierAccountResult ToResult(ApplicationUser user) =>
        new(
            user.Id,
            user.Email!,
            user.IsEnabled,
            user.MustChangePassword,
            user.DisabledAt,
            user.PasswordChangeRequiredAt,
            user.PasswordChangedAt);
}

public sealed record CashierAccountResult(
    Guid Id,
    string Email,
    bool IsEnabled,
    bool MustChangePassword,
    DateTimeOffset? DisabledAt,
    DateTimeOffset? PasswordChangeRequiredAt,
    DateTimeOffset? PasswordChangedAt);

public sealed record CashierCredentialResult(
    Guid Id,
    string Email,
    string TemporaryPassword,
    bool MustChangePassword,
    bool WasAlreadyCompleted);

internal static class TemporaryPasswordGenerator
{
    public static string Create()
    {
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string digits = "23456789";
        const string symbols = "!@$%*-_";
        const string all = lower + upper + digits + symbols;
        var chars = new char[20];
        chars[0] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[1] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        chars[3] = symbols[RandomNumberGenerator.GetInt32(symbols.Length)];
        for (var index = 4; index < chars.Length; index++)
        {
            chars[index] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        RandomNumberGenerator.Shuffle(chars.AsSpan());
        return new string(chars);
    }
}
