using System.Diagnostics;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Api.ErrorHandling;
using SimpleStore.Api.Security;
using SimpleStore.Api;
using SimpleStore.Application;
using SimpleStore.Application.Abstractions;
using SimpleStore.Infrastructure;
using SimpleStore.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
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
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<ForcedPasswordChangeMiddleware>();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.MapHealthChecks("/health").AllowAnonymous();
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

app.Run();

public partial class Program;
