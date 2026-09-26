using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleStore.Application.Abstractions;
using SimpleStore.Infrastructure.Identity;
using SimpleStore.Infrastructure.Persistence;

namespace SimpleStore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SimpleStore")
            ?? throw new InvalidOperationException(
                "Connection string 'SimpleStore' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
        services.AddScoped<ISlice1Repository, Slice1Repository>();
        services.AddScoped<ISlice2Repository, Slice2Repository>();
        services.AddScoped<ISlice3Repository, Slice3Repository>();
        services.AddScoped<ISlice4Repository, Slice4Repository>();
        services.AddScoped<ISlice5Repository, Slice5Repository>();
        services.AddScoped<Slice5BRepository>();
        services.AddScoped<ISlice5BRepository>(provider =>
            provider.GetRequiredService<Slice5BRepository>());
        services.AddScoped<ISlice6ARepository>(provider =>
            provider.GetRequiredService<Slice5BRepository>());
        services.AddScoped<ISlice6BRepository, Slice6BRepository>();
        services.AddScoped<IPilotReadinessRepository, PilotReadinessRepository>();
        services.AddScoped<AccountManagementService>();
        services.AddScoped<OwnerBootstrapService>();

        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "__Host-SimpleStore.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.Path = "/";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync,
                OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                },
                OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }
            };
        });
        services.Configure<SecurityStampValidatorOptions>(options =>
            options.ValidationInterval = TimeSpan.Zero);

        return services;
    }
}
