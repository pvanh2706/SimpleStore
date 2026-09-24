namespace SimpleStore.Domain.Experiments;

public sealed class C14ExperimentEvent
{
    public const int MaxEventTypeLength = 32;
    public const int MaxAttentionKindLength = 32;

    private C14ExperimentEvent()
    {
    }

    private C14ExperimentEvent(
        Guid eventId,
        Guid storeId,
        Guid actorUserId,
        string eventType,
        Guid? productId,
        string? attentionKind,
        DateOnly businessDate,
        DateTimeOffset occurredAt)
    {
        EventId = eventId;
        StoreId = storeId;
        ActorUserId = actorUserId;
        EventType = eventType;
        ProductId = productId;
        AttentionKind = attentionKind;
        BusinessDate = businessDate;
        OccurredAt = occurredAt;
    }

    public Guid EventId { get; private set; }

    public Guid StoreId { get; private set; }

    public Guid ActorUserId { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public Guid? ProductId { get; private set; }

    public string? AttentionKind { get; private set; }

    public DateOnly BusinessDate { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static C14ExperimentEvent Create(
        Guid eventId,
        Guid storeId,
        Guid actorUserId,
        string eventType,
        Guid? productId,
        string? attentionKind,
        DateOnly businessDate,
        DateTimeOffset occurredAt)
    {
        if (eventId == Guid.Empty)
        {
            throw new DomainRuleException("event-id-required", "EventId is required.");
        }

        if (storeId == Guid.Empty || actorUserId == Guid.Empty)
        {
            throw new DomainRuleException("c14-event-identity-required", "Store and actor are required.");
        }

        if (!C14ExperimentEventTypes.All.Contains(eventType))
        {
            throw new DomainRuleException("invalid-c14-event-type", "C14 event type is invalid.");
        }

        if (eventType == C14ExperimentEventTypes.TodayOpened)
        {
            if (productId.HasValue || attentionKind is not null)
            {
                throw new DomainRuleException(
                    "invalid-c14-event-target",
                    "TodayOpened must not identify a product or attention kind.");
            }
        }
        else
        {
            if (!productId.HasValue || productId == Guid.Empty)
            {
                throw new DomainRuleException(
                    "c14-event-product-required",
                    "This C14 event requires a product.");
            }

            if (attentionKind is null || !C14AttentionKinds.All.Contains(attentionKind))
            {
                throw new DomainRuleException(
                    "invalid-c14-attention-kind",
                    "This C14 event requires a valid attention kind.");
            }
        }

        return new C14ExperimentEvent(
            eventId,
            storeId,
            actorUserId,
            eventType,
            productId,
            attentionKind,
            businessDate,
            occurredAt);
    }

    public bool HasSameIdentity(
        Guid storeId,
        Guid actorUserId,
        string eventType,
        Guid? productId,
        string? attentionKind) =>
        StoreId == storeId
        && ActorUserId == actorUserId
        && EventType == eventType
        && ProductId == productId
        && AttentionKind == attentionKind;
}

public static class C14ExperimentEventTypes
{
    public const string TodayOpened = "TodayOpened";
    public const string SignalShown = "SignalShown";
    public const string WhyOpened = "WhyOpened";
    public const string PurchaseDraftStarted = "PurchaseDraftStarted";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(
        [TodayOpened, SignalShown, WhyOpened, PurchaseDraftStarted],
        StringComparer.Ordinal);
}

public static class C14AttentionKinds
{
    public const string NegativeStock = "NegativeStock";
    public const string OutOfStock = "OutOfStock";
    public const string LowStockRisk = "LowStockRisk";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(
        [NegativeStock, OutOfStock, LowStockRisk],
        StringComparer.Ordinal);
}
