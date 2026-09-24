using SimpleStore.Domain.Experiments;

namespace SimpleStore.Application.Abstractions;

public interface ISlice6BRepository
{
    Task<IReadOnlyList<C14AttentionCandidateData>> GetAttentionCandidatesAsync(
        Guid storeId,
        Guid warehouseId,
        DateTimeOffset velocityStartUtc,
        DateTimeOffset velocityEndUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<C14SourceActivity>> GetAttentionEvidenceAsync(
        Guid storeId,
        Guid productId,
        DateTimeOffset velocityStartUtc,
        DateTimeOffset velocityEndUtc,
        CancellationToken cancellationToken);

    Task<bool> IsActiveProductAsync(
        Guid storeId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<C14ExperimentEvent?> GetExperimentEventAsync(
        Guid eventId,
        CancellationToken cancellationToken);

    void AddExperimentEvent(C14ExperimentEvent experimentEvent);

    Task<T> ExecuteExperimentEventTransactionAsync<T>(
        Guid eventId,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);
}

public sealed record C14AttentionCandidateData(
    Guid ProductId,
    string ProductName,
    string NormalizedProductName,
    string Sku,
    string Unit,
    DateTimeOffset ProductCreatedAt,
    decimal CurrentStock,
    decimal SaleQuantity,
    decimal ReturnQuantity,
    decimal SaleVoidQuantity);

public sealed record C14SourceActivity(
    string SourceType,
    Guid SourceId,
    Guid? RelatedAggregateId,
    DateTimeOffset OccurredAt,
    Guid ProductId,
    decimal QuantityContribution,
    string NavigationType,
    Guid NavigationId);
