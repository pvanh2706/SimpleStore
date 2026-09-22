using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SimpleStore.Application.Errors;

namespace SimpleStore.Infrastructure.Persistence;

internal static class ApplicationLock
{
    public static Task AcquireBusinessOperationAsync(
        ApplicationDbContext dbContext,
        Guid operationId,
        CancellationToken cancellationToken) =>
        AcquireAsync(dbContext, $"SimpleStore:BusinessOperation:{operationId:N}", cancellationToken);

    public static Task AcquireSaleCorrectionAsync(
        ApplicationDbContext dbContext,
        Guid saleId,
        CancellationToken cancellationToken) =>
        AcquireAsync(dbContext, $"SimpleStore:SaleCorrection:{saleId:N}", cancellationToken);

    public static Task AcquirePurchaseCorrectionAsync(
        ApplicationDbContext dbContext,
        Guid purchaseId,
        CancellationToken cancellationToken) =>
        AcquireAsync(dbContext, $"SimpleStore:PurchaseCorrection:{purchaseId:N}", cancellationToken);

    public static Task AcquireCustomerDebtAsync(
        ApplicationDbContext dbContext,
        Guid storeId,
        Guid customerId,
        CancellationToken cancellationToken) =>
        AcquireAsync(
            dbContext,
            $"SimpleStore:CustomerDebt:{storeId:N}:{customerId:N}",
            cancellationToken);

    public static Task AcquireSupplierDebtAsync(
        ApplicationDbContext dbContext,
        Guid storeId,
        Guid supplierId,
        CancellationToken cancellationToken) =>
        AcquireAsync(
            dbContext,
            $"SimpleStore:SupplierDebt:{storeId:N}:{supplierId:N}",
            cancellationToken);

    private static async Task AcquireAsync(
        ApplicationDbContext dbContext,
        string resource,
        CancellationToken cancellationToken)
    {
        var transaction = dbContext.Database.CurrentTransaction
            ?? throw new InvalidOperationException("An active database transaction is required.");
        var command = dbContext.Database.GetDbConnection().CreateCommand();
        await using (command)
        {
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = """
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock
                    @Resource = @resource,
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Transaction',
                    @LockTimeout = 15000;
                SELECT @result;
                """;
            command.Parameters.Add(new SqlParameter("@resource", SqlDbType.NVarChar, 255) { Value = resource });
            var result = Convert.ToInt32(
                await command.ExecuteScalarAsync(cancellationToken),
                System.Globalization.CultureInfo.InvariantCulture);
            if (result < 0)
            {
                throw new ApplicationConflictException(
                    "operation-lock-timeout",
                    "The operation is already being processed. Check its status and retry.");
            }
        }
    }
}
