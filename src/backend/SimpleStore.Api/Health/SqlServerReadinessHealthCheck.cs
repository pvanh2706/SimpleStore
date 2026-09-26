using System.Data;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SimpleStore.Infrastructure.Persistence;

namespace SimpleStore.Api.Health;

public sealed partial class SqlServerReadinessHealthCheck(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<SqlServerReadinessHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var timeoutSeconds = Math.Clamp(
            configuration.GetValue("Readiness:SqlTimeoutSeconds", 5),
            1,
            30);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var connection = dbContext.Database.GetDbConnection();
            await connection.OpenAsync(timeout.Token);
            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                command.CommandType = CommandType.Text;
                command.CommandTimeout = timeoutSeconds;
                var result = await command.ExecuteScalarAsync(timeout.Token);
                return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture) == 1
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy("SQL readiness query returned an unexpected result.");
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException
            || !cancellationToken.IsCancellationRequested)
        {
            LogReadinessFailure(
                logger,
                Activity.Current?.Id ?? "unavailable",
                exception.GetType().Name);
            return HealthCheckResult.Unhealthy("SQL Server is unavailable.");
        }
    }

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Warning,
        Message = "SQL readiness check failed. TraceId: {TraceId}; FailureType: {FailureType}")]
    private static partial void LogReadinessFailure(
        ILogger logger,
        string traceId,
        string failureType);
}
