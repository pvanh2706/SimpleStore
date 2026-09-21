using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleStore.Infrastructure.Persistence;
using Xunit;

namespace SimpleStore.IntegrationTests;

public sealed class ApiFoundationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task HealthEndpointReturnsOk()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiEndpointReturnsDocumentInDevelopment()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public void PersistenceUsesSqlServerProvider()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", dbContext.Database.ProviderName);
    }

    [Fact]
    public void ApplicationCookieIsHttpOnlyAndSecure()
    {
        var cookieOptions = factory.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);

        Assert.True(cookieOptions.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, cookieOptions.Cookie.SecurePolicy);
    }

    [Fact]
    public void AntiforgeryCookieIsHostPrefixCompatible()
    {
        var antiforgeryOptions = factory.Services
            .GetRequiredService<IOptions<AntiforgeryOptions>>()
            .Value;

        Assert.Equal("X-CSRF-TOKEN", antiforgeryOptions.HeaderName);
        Assert.Equal("__Host-SimpleStore.Antiforgery", antiforgeryOptions.Cookie.Name);
        Assert.True(antiforgeryOptions.Cookie.HttpOnly);
        Assert.Equal("/", antiforgeryOptions.Cookie.Path);
        Assert.Null(antiforgeryOptions.Cookie.Domain);
        Assert.Equal(SameSiteMode.Strict, antiforgeryOptions.Cookie.SameSite);
        Assert.Equal(CookieSecurePolicy.Always, antiforgeryOptions.Cookie.SecurePolicy);
    }

    [Fact]
    public async Task AntiforgeryEndpointAllowsAnonymousAccessAndReturnsTokenCookiePair()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/api/security/antiforgery");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var responseBody = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var requestToken = responseBody.RootElement.GetProperty("requestToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(requestToken));

        var antiforgeryCookie = Assert.Single(
            response.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(
                "__Host-SimpleStore.Antiforgery=",
                StringComparison.Ordinal));
        var cookieParts = antiforgeryCookie
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Assert.Contains(cookieParts, part => part.Equals("path=/", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(cookieParts, part => part.Equals("secure", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(cookieParts, part => part.Equals("httponly", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(cookieParts, part => part.Equals("samesite=strict", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            cookieParts,
            part => part.StartsWith("domain=", StringComparison.OrdinalIgnoreCase));
    }
}
