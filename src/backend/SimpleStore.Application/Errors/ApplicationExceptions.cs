namespace SimpleStore.Application.Errors;

public sealed record ValidationError(
    int? RowNumber,
    string Field,
    string Code,
    string Message);

public sealed class ApplicationValidationException(
    string code,
    string message,
    IReadOnlyCollection<ValidationError> errors) : Exception(message)
{
    public string Code { get; } = code;

    public IReadOnlyCollection<ValidationError> Errors { get; } = errors;
}

public sealed class ApplicationConflictException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class ApplicationNotFoundException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed record StockShortage(Guid ProductId, decimal ShortageQuantity);

public sealed class InsufficientStockException(IReadOnlyCollection<StockShortage> shortages)
    : Exception("There is not enough stock to complete the sale.")
{
    public IReadOnlyCollection<StockShortage> Shortages { get; } = shortages;
}

public sealed class UniqueConstraintException(string message, Exception innerException)
    : Exception(message, innerException);
