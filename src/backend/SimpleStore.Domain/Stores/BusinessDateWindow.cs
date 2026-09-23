namespace SimpleStore.Domain.Stores;

public sealed record BusinessDateWindow(
    DateOnly BusinessDate,
    string TimeZoneId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc)
{
    public static BusinessDateWindow Resolve(DateOnly businessDate, string timeZoneId)
    {
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone)
            || !timeZone.HasIanaId)
        {
            throw new DomainRuleException(
                "invalid-store-timezone",
                "Store timezone must be a valid canonical IANA timezone ID.");
        }

        DateTime startLocal;
        DateTime endLocal;
        try
        {
            startLocal = businessDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            endLocal = businessDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new DomainRuleException(
                "invalid-business-date",
                "The business date cannot be resolved to a complete local-day window.");
        }

        if (timeZone.IsInvalidTime(startLocal) || timeZone.IsInvalidTime(endLocal)
            || timeZone.IsAmbiguousTime(startLocal) || timeZone.IsAmbiguousTime(endLocal))
        {
            throw new DomainRuleException(
                "invalid-business-date",
                "A business-date midnight cannot be resolved unambiguously in the configured timezone.");
        }

        return new BusinessDateWindow(
            businessDate,
            timeZone.Id,
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(startLocal, timeZone), TimeSpan.Zero),
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(endLocal, timeZone), TimeSpan.Zero));
    }
}
