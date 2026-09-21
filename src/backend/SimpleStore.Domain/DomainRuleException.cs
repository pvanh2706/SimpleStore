namespace SimpleStore.Domain;

public sealed class DomainRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
