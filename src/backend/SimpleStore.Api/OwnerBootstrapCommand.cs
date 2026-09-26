using System.Reflection;
using System.Text.Json;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Api;

internal static class OwnerBootstrapCommand
{
    private const string CommandName = "bootstrap-owner";

    public static bool IsRequested(string[] args) =>
        args.Length > 0 && string.Equals(args[0], CommandName, StringComparison.OrdinalIgnoreCase);

    public static async Task<int> RunAsync(
        string[] args,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var email = GetOption(args, "--email") ?? string.Empty;
        string password;
        if (args.Contains("--password-stdin", StringComparer.OrdinalIgnoreCase))
        {
            password = (await Console.In.ReadLineAsync(cancellationToken)) ?? string.Empty;
        }
        else if (!Console.IsInputRedirected)
        {
            password = ReadSecret("Password: ");
        }
        else
        {
            password = string.Empty;
        }

        OwnerBootstrapResult result;
        try
        {
            await using var scope = services.CreateAsyncScope();
            var bootstrap = scope.ServiceProvider.GetRequiredService<OwnerBootstrapService>();
            result = await bootstrap.BootstrapAsync(email, password, cancellationToken);
        }
        catch
        {
            result = new OwnerBootstrapResult(false, email.Trim().ToUpperInvariant(), false);
        }

        var entryAssembly = Assembly.GetEntryAssembly();
        var version = entryAssembly?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";
        var commitSha = entryAssembly?
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "RepositoryCommit")?
            .Value;
        if (string.IsNullOrWhiteSpace(commitSha))
        {
            var separator = version.LastIndexOf('+');
            commitSha = separator >= 0 && separator < version.Length - 1
                ? version[(separator + 1)..]
                : "unknown";
        }
        Console.Out.WriteLine(JsonSerializer.Serialize(new
        {
            timestamp = DateTimeOffset.UtcNow,
            normalizedOwnerIdentity = result.NormalizedEmail,
            result = result.Success ? (result.WasAlreadyBootstrapped ? "AlreadyBootstrapped" : "Success") : "Failure",
            version,
            commitSha
        }));
        return result.Success ? 0 : 1;
    }

    private static string? GetOption(string[] args, string name)
    {
        for (var index = 1; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static string ReadSecret(string prompt)
    {
        Console.Error.Write(prompt);
        var characters = new List<char>();
        while (Console.ReadKey(intercept: true) is var key && key.Key != ConsoleKey.Enter)
        {
            if (key.Key == ConsoleKey.Backspace)
            {
                if (characters.Count > 0)
                {
                    characters.RemoveAt(characters.Count - 1);
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                characters.Add(key.KeyChar);
            }
        }

        Console.Error.WriteLine();
        return new string(characters.ToArray());
    }
}
