using Microsoft.Extensions.DependencyInjection;
using SimpleStore.Application.ProductImports;
using SimpleStore.Application.Products;
using SimpleStore.Application.Purchases;
using SimpleStore.Application.Stores;
using SimpleStore.Application.Suppliers;
using SimpleStore.Application.Customers;
using SimpleStore.Application.Sales;
using SimpleStore.Application.Returns;
using SimpleStore.Application.Corrections;
using SimpleStore.Application.Debts;
using SimpleStore.Application.Reports;

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
        services.AddScoped<CreateCustomerUseCase>();
        services.AddScoped<GetCustomerUseCase>();
        services.AddScoped<GetCustomersUseCase>();
        services.AddScoped<CompleteSaleUseCase>();
        services.AddScoped<GetSaleUseCase>();
        services.AddScoped<GetSalesUseCase>();
        services.AddScoped<GetStoreOperationalSettingsUseCase>();
        services.AddScoped<UpdateNegativeStockPolicyUseCase>();
        services.AddScoped<UpdateStoreTimeZoneUseCase>();
        services.AddScoped<PreviewReturnUseCase>();
        services.AddScoped<CreateReturnUseCase>();
        services.AddScoped<GetReturnUseCase>();
        services.AddScoped<GetReturnContextUseCase>();
        services.AddScoped<VoidSaleUseCase>();
        services.AddScoped<VoidPurchaseUseCase>();
        services.AddScoped<GetCustomerDebtUseCase>();
        services.AddScoped<GetCustomerDebtsUseCase>();
        services.AddScoped<RecordCustomerDebtPaymentUseCase>();
        services.AddScoped<GetSupplierDebtUseCase>();
        services.AddScoped<GetSupplierDebtsUseCase>();
        services.AddScoped<RecordSupplierDebtPaymentUseCase>();
        services.AddScoped<GetEndOfDayReportUseCase>();
        services.AddScoped<TodayContextResolver>();
        services.AddScoped<GetTodaySummaryUseCase>();
        services.AddScoped<GetTodayExplanationUseCase>();

        return services;
    }
}
