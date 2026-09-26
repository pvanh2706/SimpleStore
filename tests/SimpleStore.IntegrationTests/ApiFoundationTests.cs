using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using SimpleStore.Infrastructure.Persistence;
using Xunit;

namespace SimpleStore.IntegrationTests;

public sealed class ApiFoundationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task HealthEndpointsReturnGenericHealthyStateWhenDatabaseIsAvailable()
    {
        using var client = factory.CreateClient();

        foreach (var path in new[] { "/health/live", "/health/ready", "/health" })
        {
            using var response = await client.GetAsync(path);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task LivenessRemainsHealthyAndReadinessIsGenericWhenDatabaseIsUnavailable()
    {
        await using var unavailableFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:SimpleStore"] =
                            "Server=127.0.0.1,1;Database=SensitiveDatabaseName;User Id=SensitiveUser;Password=SensitivePassword;Encrypt=False;Connect Timeout=1",
                        ["OperationalLogging:Enabled"] = "false",
                        ["Readiness:SqlTimeoutSeconds"] = "1"
                    }));
            });
        using var client = unavailableFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var live = await client.GetAsync("/health/live");
        using var ready = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal("Healthy", await live.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        var body = await ready.Content.ReadAsStringAsync();
        Assert.Equal("Unhealthy", body);
        Assert.DoesNotContain("Sensitive", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("127.0.0.1", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connection", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VersionEndpointIsAuthenticatedAndReturnsOnlySafeMetadata()
    {
        using var anonymousClient = factory.CreateHttpsClient();
        using var anonymousResponse = await anonymousClient.GetAsync("/api/system/version");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        var credentials = await factory.CreateOwnerAsync();
        using var client = factory.CreateHttpsClient();
        await client.LoginAsync(credentials.Email, credentials.Password);
        using var response = await client.GetAsync("/api/system/version");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var properties = body.RootElement.EnumerateObject().ToArray();
        Assert.Equal(3, properties.Length);
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("applicationVersion").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("commitSha").GetString()));
        Assert.Equal("Development", body.RootElement.GetProperty("environment").GetString());
        Assert.DoesNotContain(properties, property =>
            property.Name.Contains("connection", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("host", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("database", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("path", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SpaFallbackServesDirectNavigationButNeverSwallowsApiOrHealthRoutes()
    {
        using var client = factory.CreateHttpsClient();

        using var spa = await client.GetAsync("/products/direct-navigation");
        using var unknownApi = await client.GetAsync("/api/not-a-real-endpoint");
        using var unknownHealth = await client.GetAsync("/health/not-a-real-endpoint");

        Assert.Equal(HttpStatusCode.OK, spa.StatusCode);
        Assert.Equal("text/html", spa.Content.Headers.ContentType?.MediaType);
        Assert.Contains("SimpleStore test SPA", await spa.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, unknownApi.StatusCode);
        Assert.Equal("application/problem+json", unknownApi.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain(
            "SimpleStore test SPA",
            await unknownApi.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, unknownHealth.StatusCode);
        Assert.DoesNotContain(
            "SimpleStore test SPA",
            await unknownHealth.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);
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
