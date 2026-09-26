using System.Diagnostics;

namespace SimpleStore.Api.Operations;

public static class TraceCorrelation
{
    public static string GetTraceId(HttpContext context) =>
        Activity.Current?.Id ?? context.TraceIdentifier;
}
