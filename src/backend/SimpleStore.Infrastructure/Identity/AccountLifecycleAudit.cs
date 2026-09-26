namespace SimpleStore.Infrastructure.Identity;

public sealed class AccountLifecycleAudit
{
    private AccountLifecycleAudit()
    {
    }

    private AccountLifecycleAudit(
        Guid id,
        Guid storeId,
        Guid targetUserId,
        AccountLifecycleAction action,
        Guid performedByUserId,
        DateTimeOffset occurredAt)
    {
        Id = id;
        StoreId = storeId;
        TargetUserId = targetUserId;
        Action = action;
        PerformedByUserId = performedByUserId;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid TargetUserId { get; private set; }
    public AccountLifecycleAction Action { get; private set; }
    public Guid PerformedByUserId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    public static AccountLifecycleAudit Create(
        Guid storeId,
        Guid targetUserId,
        AccountLifecycleAction action,
        Guid performedByUserId,
        DateTimeOffset occurredAt) =>
        new(Guid.NewGuid(), storeId, targetUserId, action, performedByUserId, occurredAt);
}

public enum AccountLifecycleAction
{
    CashierCreated = 1,
    CashierDisabled = 2,
    CredentialReset = 3,
    CredentialChanged = 4
}
