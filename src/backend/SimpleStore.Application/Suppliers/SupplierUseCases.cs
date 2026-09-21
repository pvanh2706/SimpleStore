using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Errors;
using SimpleStore.Application.Stores;
using SimpleStore.Domain.Suppliers;

namespace SimpleStore.Application.Suppliers;

public sealed class CreateSupplierUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice2Repository repository,
    TimeProvider timeProvider)
{
    public async Task<SupplierResult> ExecuteAsync(
        SupplierWriteCommand command,
        CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            slice1Repository,
            cancellationToken);
        var supplier = Supplier.Create(
            storeId,
            command.Name,
            command.Phone,
            command.Note,
            timeProvider.GetUtcNow());
        repository.AddSupplier(supplier);
        await repository.SaveChangesAsync(cancellationToken);
        return SupplierUseCaseSupport.ToResult(supplier, 0);
    }
}

public sealed class UpdateSupplierUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice2Repository repository,
    TimeProvider timeProvider)
{
    public async Task<SupplierResult> ExecuteAsync(
        Guid supplierId,
        SupplierWriteCommand command,
        CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            slice1Repository,
            cancellationToken);
        var supplier = await SupplierUseCaseSupport.GetRequiredAsync(
            repository,
            storeId,
            supplierId,
            cancellationToken);
        supplier.Update(command.Name, command.Phone, command.Note, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        var outstanding = await repository.GetSupplierOutstandingAsync(
            storeId,
            supplierId,
            cancellationToken);
        return SupplierUseCaseSupport.ToResult(supplier, outstanding);
    }
}

public sealed class DeactivateSupplierUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice2Repository repository,
    TimeProvider timeProvider)
{
    public async Task ExecuteAsync(Guid supplierId, CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            slice1Repository,
            cancellationToken);
        var supplier = await SupplierUseCaseSupport.GetRequiredAsync(
            repository,
            storeId,
            supplierId,
            cancellationToken);
        supplier.Deactivate(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
    }
}

public sealed class GetSupplierUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice2Repository repository)
{
    public async Task<SupplierResult> ExecuteAsync(
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            slice1Repository,
            cancellationToken);
        var supplier = await SupplierUseCaseSupport.GetRequiredAsync(
            repository,
            storeId,
            supplierId,
            cancellationToken);
        var outstanding = await repository.GetSupplierOutstandingAsync(
            storeId,
            supplierId,
            cancellationToken);
        return SupplierUseCaseSupport.ToResult(supplier, outstanding);
    }
}

public sealed class GetSuppliersUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice2Repository repository)
{
    public async Task<SupplierListResult> ExecuteAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        PaginationGuard.Validate(page, pageSize);
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            slice1Repository,
            cancellationToken);
        var result = await repository.SearchSuppliersAsync(
            storeId,
            search?.Trim(),
            isActive,
            page,
            pageSize,
            cancellationToken);
        return new SupplierListResult(
            result.Items.Select(item => SupplierUseCaseSupport.ToResult(
                item.Supplier,
                item.OutstandingAmount)).ToArray(),
            page,
            pageSize,
            result.TotalCount,
            PageCount(result.TotalCount, pageSize));
    }

    private static int PageCount(int totalCount, int pageSize) =>
        totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
}

internal static class SupplierUseCaseSupport
{
    public static async Task<Supplier> GetRequiredAsync(
        ISlice2Repository repository,
        Guid storeId,
        Guid supplierId,
        CancellationToken cancellationToken) =>
        await repository.GetSupplierAsync(storeId, supplierId, cancellationToken)
        ?? throw new ApplicationNotFoundException("supplier-not-found", "Supplier was not found.");

    public static SupplierResult ToResult(Supplier supplier, decimal outstandingAmount) =>
        new(
            supplier.Id,
            supplier.Name,
            supplier.Phone,
            supplier.Note,
            supplier.IsActive,
            outstandingAmount,
            supplier.CreatedAt,
            supplier.UpdatedAt);
}

internal static class PaginationGuard
{
    public static void Validate(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw new ApplicationValidationException(
                "invalid-pagination",
                "Pagination values are invalid.",
                [new ValidationError(null, "page", "invalid-pagination", "Page must be at least 1 and page size must be between 1 and 100.")]);
        }
    }
}
