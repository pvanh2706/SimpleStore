using System.Diagnostics;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Api.ErrorHandling;
using SimpleStore.Api.Health;
using SimpleStore.Api.Operations;
using SimpleStore.Api.Security;
using SimpleStore.Api;
using SimpleStore.Application;
using SimpleStore.Application.Abstractions;
using SimpleStore.Infrastructure;
using SimpleStore.Infrastructure.Identity;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, loggerConfiguration) =>
{
    var options = context.Configuration
        .GetSection("OperationalLogging")
        .Get<OperationalLoggingOptions>() ?? new OperationalLoggingOptions();
    var retentionDays = Math.Clamp(options.RetentionDays, 1, 365);
    var fileSizeLimit = Math.Clamp(options.FileSizeLimitBytes, 1_048_576, 1_073_741_824);

    loggerConfiguration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console(new JsonFormatter(renderMessage: true));

    if (options.Enabled)
    {
        var path = string.IsNullOrWhiteSpace(options.Path)
            ? OperationalLoggingOptions.GetDefaultPath(context.HostingEnvironment)
            : options.Path;
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        loggerConfiguration.WriteTo.File(
            new JsonFormatter(renderMessage: true),
            path,
            rollingInterval: RollingInterval.Day,
            fileSizeLimitBytes: fileSizeLimit,
            rollOnFileSizeLimit: true,
            retainedFileCountLimit: null,
            retainedFileTimeLimit: TimeSpan.FromDays(retentionDays),
            shared: true,
            flushToDiskInterval: TimeSpan.FromSeconds(1));
    }
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddOpenApi();
builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), ["live"])
    .AddCheck<SqlServerReadinessHealthCheck>("sql-server", tags: ["ready"]);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            TraceCorrelation.GetTraceId(context.HttpContext);
    };
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__Host-SimpleStore.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.Path = "/";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddSingleton<IApplicationBuildInfo, ApplicationBuildInfo>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (OwnerBootstrapCommand.IsRequested(args))
{
    Environment.ExitCode = await OwnerBootstrapCommand.RunAsync(
        args,
        app.Services,
        CancellationToken.None);
    await app.DisposeAsync();
    return;
}

if (app.Environment.IsDevelopment())
{
    await app.Services.SeedDevelopmentOwnerAsync(builder.Configuration, app.Logger);
}

app.UseExceptionHandler();
app.UseStatusCodePages(async statusCodeContext =>
{
    await Results.Problem(statusCode: statusCodeContext.HttpContext.Response.StatusCode)
        .ExecuteAsync(statusCodeContext.HttpContext);
});
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        var buildInfo = httpContext.RequestServices.GetRequiredService<IApplicationBuildInfo>();
        diagnosticContext.Set("TraceId", TraceCorrelation.GetTraceId(httpContext));
        diagnosticContext.Set("RequestMethod", httpContext.Request.Method);
        diagnosticContext.Set("RequestPath", httpContext.Request.Path.Value ?? string.Empty);
        diagnosticContext.Set("RouteTemplate",
            httpContext.GetEndpoint()?.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.RouteNameMetadata>()?.RouteName
            ?? httpContext.GetEndpoint()?.DisplayName
            ?? string.Empty);
        diagnosticContext.Set("UserId",
            httpContext.Items.TryGetValue(OperationalContext.UserIdItem, out var userId)
                ? userId
                : null);
        diagnosticContext.Set("StoreId",
            httpContext.Items.TryGetValue(OperationalContext.StoreIdItem, out var storeId)
                ? storeId
                : null);
        diagnosticContext.Set("ApplicationVersion", buildInfo.ApplicationVersion);
        diagnosticContext.Set("CommitSha", buildInfo.CommitSha);
        diagnosticContext.Set("Environment", buildInfo.Environment);
    };
});
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseMiddleware<ForcedPasswordChangeMiddleware>();
app.UseMiddleware<RequestLoggingContextMiddleware>();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

var healthOptions = new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
        || registration.Tags.Contains("ready"),
    ResponseWriter = GenericHealthResponseWriter.WriteAsync
};
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    ResponseWriter = GenericHealthResponseWriter.WriteAsync
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", healthOptions).AllowAnonymous();
app.MapHealthChecks("/health", healthOptions).AllowAnonymous();
app.MapGet(
        "/api/security/antiforgery",
        (HttpContext httpContext, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(httpContext);
            var requestToken = tokens.RequestToken
                ?? throw new InvalidOperationException("Antiforgery request token was not generated.");

            httpContext.Response.Headers.CacheControl = "no-store";

            return Results.Ok(new { requestToken });
        })
    .AllowAnonymous();
app.MapControllers();

app.MapFallback(async context =>
{
    var path = context.Request.Path;
    if (path.StartsWithSegments("/api")
        || path.StartsWithSegments("/health")
        || Path.HasExtension(path.Value))
    {
        await Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "The requested resource was not found.")
            .ExecuteAsync(context);
        return;
    }

    var indexPath = Path.Combine(app.Environment.WebRootPath ?? string.Empty, "index.html");
    if (!File.Exists(indexPath))
    {
        await Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "The application frontend is unavailable.")
            .ExecuteAsync(context);
        return;
    }

    await Results.File(indexPath, "text/html; charset=utf-8").ExecuteAsync(context);
}).AllowAnonymous();

var applicationBuildInfo = app.Services.GetRequiredService<IApplicationBuildInfo>();
ApplicationLifecycleLog.Starting(
    app.Logger,
    applicationBuildInfo.ApplicationVersion,
    applicationBuildInfo.CommitSha,
    applicationBuildInfo.Environment);
app.Lifetime.ApplicationStopping.Register(() => ApplicationLifecycleLog.Stopping(
        app.Logger,
        applicationBuildInfo.ApplicationVersion,
        applicationBuildInfo.CommitSha,
        applicationBuildInfo.Environment));

app.Run();

public partial class Program;
