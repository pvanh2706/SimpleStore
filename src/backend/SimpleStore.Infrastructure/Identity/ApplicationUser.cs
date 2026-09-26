using Microsoft.AspNetCore.Identity;

namespace SimpleStore.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? StoreId { get; private set; }

    public bool IsEnabled { get; private set; } = true;

    public bool MustChangePassword { get; private set; }

    public DateTimeOffset? DisabledAt { get; private set; }

    public DateTimeOffset? PasswordChangeRequiredAt { get; private set; }

    public DateTimeOffset? PasswordChangedAt { get; private set; }

    public void AssignToStore(Guid storeId)
    {
        if (StoreId is not null && StoreId != storeId)
        {
            throw new InvalidOperationException("User is already assigned to another store.");
        }

        StoreId = storeId;
    }

    public void RequirePasswordChange(DateTimeOffset requiredAt)
    {
        MustChangePassword = true;
        PasswordChangeRequiredAt = requiredAt;
    }

    public void CompletePasswordChange(DateTimeOffset changedAt)
    {
        MustChangePassword = false;
        PasswordChangeRequiredAt = null;
        PasswordChangedAt = changedAt;
    }

    public void Disable(DateTimeOffset disabledAt)
    {
        IsEnabled = false;
        DisabledAt = disabledAt;
    }
}
