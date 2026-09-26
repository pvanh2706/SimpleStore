using System.Reflection;

namespace SimpleStore.Api.Operations;

public interface IApplicationBuildInfo
{
    string ApplicationVersion { get; }

    string CommitSha { get; }

    string Environment { get; }
}

public sealed class ApplicationBuildInfo : IApplicationBuildInfo
{
    public ApplicationBuildInfo(IWebHostEnvironment environment)
    {
        var assembly = typeof(Program).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";
        var separator = informationalVersion.IndexOf('+', StringComparison.Ordinal);

        ApplicationVersion = separator > 0
            ? informationalVersion[..separator]
            : informationalVersion;
        CommitSha = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "RepositoryCommit")?
            .Value ?? (separator >= 0 && separator < informationalVersion.Length - 1
                ? informationalVersion[(separator + 1)..]
                : "unknown");
        Environment = environment.EnvironmentName;
    }

    public string ApplicationVersion { get; }

    public string CommitSha { get; }

    public string Environment { get; }
}
