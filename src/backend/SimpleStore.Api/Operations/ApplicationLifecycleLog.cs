namespace SimpleStore.Api.Operations;

public static partial class ApplicationLifecycleLog
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "SimpleStore starting. ApplicationVersion: {ApplicationVersion}; CommitSha: {CommitSha}; Environment: {Environment}")]
    public static partial void Starting(
        ILogger logger,
        string applicationVersion,
        string commitSha,
        string environment);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "SimpleStore stopping. ApplicationVersion: {ApplicationVersion}; CommitSha: {CommitSha}; Environment: {Environment}")]
    public static partial void Stopping(
        ILogger logger,
        string applicationVersion,
        string commitSha,
        string environment);
}
