using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Errors;
using SimpleStore.Domain.Stores;

namespace SimpleStore.Application.Stores;

public sealed record StoreResult(Guid Id, string Name, Guid MainWarehouseId, string MainWarehouseName);

public sealed class InitializeStoreUseCase(
    ICurrentUser currentUser,
    ISlice1Repository repository,
    TimeProvider timeProvider)
{
    public async Task<StoreResult> ExecuteAsync(string name, CancellationToken cancellationToken)
    {
        var userId = CurrentUserGuard.GetRequiredUserId(currentUser);
        var existingStore = await GetExistingStoreAsync(userId, cancellationToken);
        if (existingStore is not null)
        {
            return existingStore;
        }

        try
        {
            return await repository.ExecuteInTransactionAsync(
                async transactionCancellationToken =>
                {
                    var storeId = await repository.GetUserStoreIdAsync(
                        userId,
                        transactionCancellationToken);
                    if (storeId is not null)
                    {
                        return await GetRequiredStoreResultAsync(
                            storeId.Value,
                            transactionCancellationToken);
                    }

                    var now = timeProvider.GetUtcNow();
                    var store = Store.Create(userId, name, now);
                    var warehouse = Warehouse.CreateMain(store.Id, now);

                    repository.AddStore(store);
                    repository.AddWarehouse(warehouse);
                    await repository.AssignUserToStoreAsync(
                        userId,
                        store.Id,
                        transactionCancellationToken);

                    return new StoreResult(store.Id, store.Name, warehouse.Id, warehouse.Name);
                },
                cancellationToken);
        }
        catch (UniqueConstraintException)
        {
            var concurrentlyCreatedStore = await GetExistingStoreAsync(userId, cancellationToken);
            if (concurrentlyCreatedStore is null)
            {
                throw;
            }

            return concurrentlyCreatedStore;
        }
    }

    private async Task<StoreResult?> GetExistingStoreAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var storeId = await repository.GetUserStoreIdAsync(userId, cancellationToken);
        return storeId is null
            ? null
            : await GetRequiredStoreResultAsync(storeId.Value, cancellationToken);
    }

    private async Task<StoreResult> GetRequiredStoreResultAsync(
        Guid storeId,
        CancellationToken cancellationToken)
    {
        var store = await repository.GetStoreAsync(storeId, cancellationToken)
            ?? throw new ApplicationNotFoundException("store-not-found", "Store was not found.");
        var warehouse = await repository.GetMainWarehouseAsync(storeId, cancellationToken)
            ?? throw new ApplicationNotFoundException(
                "main-warehouse-not-found",
                "Main warehouse was not found.");

        return new StoreResult(store.Id, store.Name, warehouse.Id, warehouse.Name);
    }
}

public sealed class GetCurrentStoreUseCase(
    ICurrentUser currentUser,
    ISlice1Repository repository)
{
    public async Task<StoreResult?> ExecuteAsync(CancellationToken cancellationToken)
    {
        var userId = CurrentUserGuard.GetRequiredUserId(currentUser);
        var storeId = await repository.GetUserStoreIdAsync(userId, cancellationToken);
        if (storeId is null)
        {
            return null;
        }

        var store = await repository.GetStoreAsync(storeId.Value, cancellationToken)
            ?? throw new ApplicationNotFoundException("store-not-found", "Store was not found.");
        var warehouse = await repository.GetMainWarehouseAsync(store.Id, cancellationToken)
            ?? throw new ApplicationNotFoundException(
                "main-warehouse-not-found",
                "Main warehouse was not found.");

        return new StoreResult(store.Id, store.Name, warehouse.Id, warehouse.Name);
    }
}

internal static class CurrentUserGuard
{
    public static Guid GetRequiredUserId(ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
        {
            throw new InvalidOperationException("An authenticated user is required.");
        }

        return currentUser.UserId;
    }

    public static async Task<Guid> GetRequiredStoreIdAsync(
        ICurrentUser currentUser,
        ISlice1Repository repository,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId(currentUser);
        return await repository.GetUserStoreIdAsync(userId, cancellationToken)
            ?? throw new ApplicationConflictException(
                "store-not-initialized",
                "Initialize the store before using product features.");
    }
}
