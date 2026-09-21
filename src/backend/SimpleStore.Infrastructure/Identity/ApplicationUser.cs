using Microsoft.AspNetCore.Identity;

namespace SimpleStore.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? StoreId { get; private set; }

    public void AssignToStore(Guid storeId)
    {
        if (StoreId is not null && StoreId != storeId)
        {
            throw new InvalidOperationException("User is already assigned to another store.");
        }

        StoreId = storeId;
    }
}
