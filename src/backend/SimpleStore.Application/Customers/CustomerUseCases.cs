using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Errors;
using SimpleStore.Application.Stores;
using SimpleStore.Application.Suppliers;
using SimpleStore.Domain.Customers;

namespace SimpleStore.Application.Customers;

public sealed class CreateCustomerUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice3Repository repository,
    TimeProvider timeProvider)
{
    public async Task<CustomerResult> ExecuteAsync(
        CreateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            slice1Repository,
            cancellationToken);
        var customer = Customer.Create(storeId, command.Name, command.Phone, timeProvider.GetUtcNow());
        repository.AddCustomer(customer);
        await repository.SaveChangesAsync(cancellationToken);
        return CustomerUseCaseSupport.ToResult(customer);
    }
}

public sealed class GetCustomerUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice3Repository repository)
{
    public async Task<CustomerResult> ExecuteAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            slice1Repository,
            cancellationToken);
        var customer = await repository.GetCustomerAsync(storeId, customerId, cancellationToken)
            ?? throw new ApplicationNotFoundException("customer-not-found", "Customer was not found.");
        return CustomerUseCaseSupport.ToResult(customer);
    }
}

public sealed class GetCustomersUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice3Repository repository)
{
    public async Task<CustomerListResult> ExecuteAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        PaginationGuard.Validate(page, pageSize);
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            slice1Repository,
            cancellationToken);
        var result = await repository.SearchCustomersAsync(
            storeId,
            search,
            page,
            pageSize,
            cancellationToken);
        return new CustomerListResult(
            result.Items.Select(CustomerUseCaseSupport.ToResult).ToArray(),
            page,
            pageSize,
            result.TotalCount,
            result.TotalCount == 0 ? 0 : (int)Math.Ceiling(result.TotalCount / (double)pageSize));
    }
}

internal static class CustomerUseCaseSupport
{
    public static CustomerResult ToResult(Customer customer) =>
        new(customer.Id, customer.Name, customer.Phone, customer.CreatedAt, customer.UpdatedAt);
}
