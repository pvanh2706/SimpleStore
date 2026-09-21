using Microsoft.Extensions.DependencyInjection;
using SimpleStore.Application.ProductImports;
using SimpleStore.Application.Products;
using SimpleStore.Application.Stores;

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

        return services;
    }
}
