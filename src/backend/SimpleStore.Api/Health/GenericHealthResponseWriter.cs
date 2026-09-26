using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SimpleStore.Api.Health;

public static class GenericHealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "text/plain; charset=utf-8";
        return context.Response.WriteAsync(
            report.Status == HealthStatus.Healthy ? "Healthy" : "Unhealthy");
    }
}
