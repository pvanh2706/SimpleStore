using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SimpleStore.Infrastructure.Identity;
using SimpleStore.Infrastructure.Persistence;
using Xunit;

namespace SimpleStore.IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _connectionString = CreateConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SimpleStore"] =
                    _connectionString,
                ["DevelopmentOwner:Email"] = null,
                ["DevelopmentOwner:Password"] = null
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    _connectionString,
                    sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
        });
    }

    public HttpClient CreateHttpsClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = true
    });

    public async Task<(string Email, string Password)> CreateOwnerAsync()
        => await CreateUserAsync(ApplicationRoles.Owner);

    public async Task<(string Email, string Password)> CreateCashierAsync(Guid storeId)
        => await CreateUserAsync(ApplicationRoles.Cashier, storeId);

    private async Task<(string Email, string Password)> CreateUserAsync(
        string role,
        Guid? storeId = null)
    {
        const string password = "Slice1-Test!2026";
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@example.test";

        await using var scope = Services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync(role))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            Assert.True(roleResult.Succeeded, FormatErrors(roleResult));
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };
        if (storeId.HasValue)
        {
            user.AssignToStore(storeId.Value);
        }

        var userResult = await userManager.CreateAsync(user, password);
        Assert.True(userResult.Succeeded, FormatErrors(userResult));
        var addRoleResult = await userManager.AddToRoleAsync(user, role);
        Assert.True(addRoleResult.Succeeded, FormatErrors(addRoleResult));

        return (email, password);
    }

    public async Task<TResult> WithDbContextAsync<TResult>(
        Func<ApplicationDbContext, Task<TResult>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(dbContext);
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await using (var scope = Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
        }

        await DisposeAsync();
    }

    internal static string CreateIsolatedConnectionString(string databasePrefix)
    {
        var configuredConnection = Environment.GetEnvironmentVariable(
            "SIMPLESTORE_TEST_CONNECTION_STRING");
        var builder = string.IsNullOrWhiteSpace(configuredConnection)
            ? new SqlConnectionStringBuilder
            {
                DataSource = "(localdb)\\MSSQLLocalDB",
                IntegratedSecurity = true,
                TrustServerCertificate = true
            }
            : new SqlConnectionStringBuilder(configuredConnection);

        builder.InitialCatalog = $"{databasePrefix}_{Guid.NewGuid():N}";
        builder.MultipleActiveResultSets = true;
        return builder.ConnectionString;
    }

    private static string CreateConnectionString() =>
        CreateIsolatedConnectionString("SimpleStoreTests");

    private static string FormatErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => error.Description));
}
