using Microsoft.Extensions.DependencyInjection;
using SimpleStore.Application.ProductImports;
using SimpleStore.Application.Products;
using SimpleStore.Application.Purchases;
using SimpleStore.Application.Stores;
using SimpleStore.Application.Suppliers;

namespace SimpleStore.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<InitializeStoreUseCase>();
        services.AddScoped<GetCurrentStoreUseCase>();
        services.AddScoped<CreateProductUseCase>();
        services.AddScoped<UpdateProductUseCase>();
        services.AddScoped<DeactivateProductUseCase>();
        services.AddScoped<GetProductUseCase>();
        services.AddScoped<GetProductsUseCase>();
        services.AddScoped<GetInventoryBalanceUseCase>();
        services.AddScoped<GetInventoryMovementsUseCase>();
        services.AddScoped<ValidateProductImportUseCase>();
        services.AddScoped<ConfirmProductImportUseCase>();
        services.AddScoped<CreateSupplierUseCase>();
        services.AddScoped<UpdateSupplierUseCase>();
        services.AddScoped<DeactivateSupplierUseCase>();
        services.AddScoped<GetSupplierUseCase>();
        services.AddScoped<GetSuppliersUseCase>();
        services.AddScoped<CreatePurchaseUseCase>();
        services.AddScoped<UpdatePurchaseUseCase>();
        services.AddScoped<GetPurchaseUseCase>();
        services.AddScoped<GetPurchasesUseCase>();
        services.AddScoped<CompletePurchaseUseCase>();
        services.AddScoped<GetOperationStatusUseCase>();

        return services;
    }
}
