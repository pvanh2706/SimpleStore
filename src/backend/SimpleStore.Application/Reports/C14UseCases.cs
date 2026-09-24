using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Errors;
using SimpleStore.Application.Products;
using SimpleStore.Application.Stores;
using SimpleStore.Domain.Experiments;
using SimpleStore.Domain.Stores;

namespace SimpleStore.Application.Reports;

public sealed record C14Context(
    Guid StoreId,
    Guid WarehouseId,
    DateTimeOffset StoreCreatedAt,
    BusinessDateWindow CurrentWindow,
    IReadOnlyList<BusinessDateWindow> CompletedBusinessDays)
{
    public DateTimeOffset VelocityStartUtc => CompletedBusinessDays[0].StartUtc;

    public DateTimeOffset VelocityEndUtc => CurrentWindow.StartUtc;
}

public sealed class C14ContextResolver(
    TodayContextResolver todayContextResolver,
    ISlice1Repository slice1Repository)
{
    public async Task<C14Context> ResolveAsync(CancellationToken cancellationToken)
    {
        var today = await todayContextResolver.ResolveAsync(cancellationToken);
        var store = await slice1Repository.GetStoreAsync(today.StoreId, cancellationToken)
            ?? throw new ApplicationNotFoundException("store-not-found", "Store was not found.");
        var warehouse = await ProductUseCaseSupport.GetMainWarehouseAsync(
            slice1Repository, today.StoreId, cancellationToken);
        var days = Enumerable.Range(0, 7)
            .Select(index => BusinessDateWindow.Resolve(
                today.Window.BusinessDate.AddDays(index - 7),
                today.Window.TimeZoneId))
            .ToArray();

        return new C14Context(today.StoreId, warehouse.Id, store.CreatedAt, today.Window, days);
    }
}

public sealed class GetC14AttentionListUseCase(
    C14ContextResolver contextResolver,
    ISlice6BRepository repository)
{
    public async Task<C14AttentionListResult> ExecuteAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePagination(page, pageSize);
        var context = await contextResolver.ResolveAsync(cancellationToken);
        var candidates = await repository.GetAttentionCandidatesAsync(
            context.StoreId,
            context.WarehouseId,
            context.VelocityStartUtc,
            context.VelocityEndUtc,
            cancellationToken);
        var allItems = C14Projection.Classify(context, candidates);
        var items = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToArray();

        return new C14AttentionListResult(
            context.CurrentWindow.BusinessDate,
            context.CurrentWindow.TimeZoneId,
            context.VelocityStartUtc,
            context.VelocityEndUtc,
            ToDays(context),
            context.StoreCreatedAt <= context.VelocityStartUtc
                ? C14HistoryCoverageTypes.FullSevenCompletedDays
                : C14HistoryCoverageTypes.PartialObservation,
            allItems.Count,
            page,
            pageSize,
            allItems.Count == 0 ? 0 : (int)Math.Ceiling(allItems.Count / (double)pageSize),
            items);
    }

    internal static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw new ApplicationValidationException(
                "invalid-pagination",
                "Pagination values are invalid.",
                [new ValidationError(null, "page", "invalid-pagination", "Page must be at least 1 and page size must be between 1 and 100.")]);
        }
    }

    internal static C14BusinessDateResult[] ToDays(C14Context context) =>
        context.CompletedBusinessDays
            .Select(day => new C14BusinessDateResult(day.BusinessDate, day.StartUtc, day.EndUtc))
            .ToArray();
}

public sealed class GetC14AttentionDetailUseCase(
    C14ContextResolver contextResolver,
    ISlice6BRepository repository)
{
    public async Task<C14AttentionDetailResult> ExecuteAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var context = await contextResolver.ResolveAsync(cancellationToken);
        var candidates = await repository.GetAttentionCandidatesAsync(
            context.StoreId,
            context.WarehouseId,
            context.VelocityStartUtc,
            context.VelocityEndUtc,
            cancellationToken);
        var candidate = candidates.SingleOrDefault(item => item.ProductId == productId);
        var item = C14Projection.Classify(context, candidates)
            .SingleOrDefault(result => result.ProductId == productId);
        if (candidate is null || item is null)
        {
            throw new ApplicationNotFoundException(
                "c14-attention-not-found",
                "The current attention item was not found.");
        }

        var source = await repository.GetAttentionEvidenceAsync(
            context.StoreId,
            productId,
            context.VelocityStartUtc,
            context.VelocityEndUtc,
            cancellationToken);
        if (source.Sum(value => value.QuantityContribution) != item.NetSoldQuantity)
        {
            throw new InvalidOperationException("C14 source evidence does not reconcile with net sold quantity.");
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(context.CurrentWindow.TimeZoneId);
        var evidence = source
            .OrderBy(value => value.OccurredAt)
            .ThenBy(value => value.SourceId)
            .Select(value => new C14SourceEvidenceResult(
                value.SourceType,
                value.SourceId,
                value.RelatedAggregateId,
                value.OccurredAt,
                DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(value.OccurredAt, timeZone).DateTime),
                value.ProductId,
                value.QuantityContribution,
                new TodaySourceNavigationResult(value.NavigationType, value.NavigationId)))
            .ToArray();

        return new C14AttentionDetailResult(
            item.ProductId,
            item.ProductName,
            item.Sku,
            item.Unit,
            item.AttentionKind,
            item.CurrentStock,
            context.CurrentWindow.BusinessDate,
            context.CurrentWindow.TimeZoneId,
            context.VelocityStartUtc,
            context.VelocityEndUtc,
            GetC14AttentionListUseCase.ToDays(context),
            item.NetSoldQuantity,
            item.AverageDailySales,
            item.DaysOfCover,
            item.HistoryCoverage,
            item.RecentSalesEvidence,
            item.RiskEvaluation,
            new C14FormulaInputsResult(
                candidate.SaleQuantity,
                candidate.ReturnQuantity,
                candidate.SaleVoidQuantity,
                7),
            evidence);
    }
}

public sealed class RecordC14ExperimentEventUseCase(
    ICurrentUser currentUser,
    C14ContextResolver contextResolver,
    ISlice6BRepository repository,
    TimeProvider timeProvider)
{
    public async Task<C14ExperimentEventResult> ExecuteAsync(
        RecordC14ExperimentEventCommand command,
        CancellationToken cancellationToken)
    {
        var context = await contextResolver.ResolveAsync(cancellationToken);
        var actorUserId = currentUser.UserId;

        return await repository.ExecuteExperimentEventTransactionAsync(
            command.EventId,
            async token =>
            {
                var existing = await repository.GetExperimentEventAsync(command.EventId, token);
                if (existing is not null)
                {
                    if (!existing.HasSameIdentity(
                        context.StoreId,
                        actorUserId,
                        command.EventType,
                        command.ProductId,
                        command.AttentionKind))
                    {
                        throw new ApplicationConflictException(
                            "c14-event-id-reused",
                            "EventId was already used for a different C14 event identity.");
                    }

                    return ToResult(existing, true);
                }

                var experimentEvent = C14ExperimentEvent.Create(
                    command.EventId,
                    context.StoreId,
                    actorUserId,
                    command.EventType,
                    command.ProductId,
                    command.AttentionKind,
                    context.CurrentWindow.BusinessDate,
                    timeProvider.GetUtcNow());

                if (experimentEvent.EventType != C14ExperimentEventTypes.TodayOpened)
                {
                    if (!experimentEvent.ProductId.HasValue
                        || !await repository.IsActiveProductAsync(
                            context.StoreId, experimentEvent.ProductId.Value, token))
                    {
                        throw new ApplicationNotFoundException(
                            "c14-event-product-not-found",
                            "The active product was not found.");
                    }
                }

                repository.AddExperimentEvent(experimentEvent);
                return ToResult(experimentEvent, false);
            },
            cancellationToken);
    }

    private static C14ExperimentEventResult ToResult(
        C14ExperimentEvent experimentEvent,
        bool wasAlreadyRecorded) => new(
            experimentEvent.EventId,
            experimentEvent.EventType,
            experimentEvent.ProductId,
            experimentEvent.AttentionKind,
            experimentEvent.BusinessDate,
            experimentEvent.OccurredAt,
            wasAlreadyRecorded);
}

internal static class C14Projection
{
    public static IReadOnlyList<C14AttentionItemResult> Classify(
        C14Context context,
        IReadOnlyList<C14AttentionCandidateData> candidates)
    {
        var normalizedNames = candidates.ToDictionary(
            candidate => candidate.ProductId,
            candidate => candidate.NormalizedProductName);

        return candidates
            .Select(candidate => Classify(context, candidate))
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderBy(item => IsFactual(item.AttentionKind) ? 0 : 1)
            .ThenBy(item => IsFactual(item.AttentionKind) ? 0m : item.DaysOfCover!.Value)
            .ThenBy(item => normalizedNames[item.ProductId], StringComparer.Ordinal)
            .ThenBy(item => item.ProductId)
            .ToArray();
    }

    private static C14AttentionItemResult? Classify(
        C14Context context,
        C14AttentionCandidateData candidate)
    {
        var netSold = candidate.SaleQuantity - candidate.ReturnQuantity - candidate.SaleVoidQuantity;
        var fullHistory = context.StoreCreatedAt <= context.VelocityStartUtc
            && candidate.ProductCreatedAt <= context.VelocityStartUtc;
        var historyCoverage = fullHistory
            ? C14HistoryCoverageTypes.FullSevenCompletedDays
            : C14HistoryCoverageTypes.PartialObservation;
        var recentSales = netSold > 0
            ? C14RecentSalesEvidenceTypes.PositiveNetSold
            : C14RecentSalesEvidenceTypes.NoPositiveNetSold;
        var riskEvaluation = !fullHistory
            ? C14RiskEvaluationTypes.InsufficientFullHistory
            : netSold <= 0
                ? C14RiskEvaluationTypes.NoPositiveSalesEvidence
                : C14RiskEvaluationTypes.Eligible;
        decimal? average = fullHistory && netSold > 0 ? netSold / 7m : null;
        decimal? daysOfCover = candidate.CurrentStock > 0 && average > 0
            ? candidate.CurrentStock / average.Value
            : null;

        var attentionKind = candidate.CurrentStock < 0 && netSold > 0
            ? C14AttentionKinds.NegativeStock
            : candidate.CurrentStock == 0 && netSold > 0
                ? C14AttentionKinds.OutOfStock
                : candidate.CurrentStock > 0 && fullHistory && netSold > 0 && daysOfCover <= 3m
                    ? C14AttentionKinds.LowStockRisk
                    : null;
        return attentionKind is null
            ? null
            : new C14AttentionItemResult(
                candidate.ProductId,
                candidate.ProductName,
                candidate.Sku,
                candidate.Unit,
                attentionKind,
                candidate.CurrentStock,
                netSold,
                average,
                daysOfCover,
                historyCoverage,
                recentSales,
                riskEvaluation);
    }

    private static bool IsFactual(string attentionKind) =>
        attentionKind is C14AttentionKinds.NegativeStock or C14AttentionKinds.OutOfStock;
}
