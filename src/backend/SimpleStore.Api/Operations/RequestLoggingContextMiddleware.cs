using System.Security.Claims;

namespace SimpleStore.Api.Operations;

public sealed class RequestLoggingContextMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingContextMiddleware> logger,
    IApplicationBuildInfo buildInfo)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.Items.TryGetValue(OperationalContext.UserIdItem, out var userValue)
            ? userValue
            : context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        context.Items.TryGetValue(OperationalContext.StoreIdItem, out var storeId);

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["TraceId"] = TraceCorrelation.GetTraceId(context),
            ["UserId"] = userId,
            ["StoreId"] = storeId,
            ["ApplicationVersion"] = buildInfo.ApplicationVersion,
            ["CommitSha"] = buildInfo.CommitSha,
            ["Environment"] = buildInfo.Environment
        });

        await next(context);
    }
}
