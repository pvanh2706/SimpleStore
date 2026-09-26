namespace SimpleStore.Api.Operations;

public sealed class OperationalLoggingOptions
{
    public bool Enabled { get; init; } = true;

    public string? Path { get; init; }

    public int RetentionDays { get; init; } = 14;

    public long FileSizeLimitBytes { get; init; } = 52_428_800;

    public static string GetDefaultPath(IHostEnvironment environment) =>
        environment.IsProduction() && OperatingSystem.IsWindows()
            ? System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "SimpleStore",
                "Logs",
                "simplestore-.json")
            : System.IO.Path.Combine(environment.ContentRootPath, "logs", "simplestore-.json");
}
