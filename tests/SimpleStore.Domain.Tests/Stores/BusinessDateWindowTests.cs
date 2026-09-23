using SimpleStore.Domain.Stores;
using Xunit;

namespace SimpleStore.Domain.Tests.Stores;

public sealed class BusinessDateWindowTests
{
    [Fact]
    public void ResolvesVietnamMidnightsIndependently()
    {
        var result = BusinessDateWindow.Resolve(new DateOnly(2026, 9, 23), "Asia/Ho_Chi_Minh");

        Assert.Equal(new DateTimeOffset(2026, 9, 22, 17, 0, 0, TimeSpan.Zero), result.StartUtc);
        Assert.Equal(new DateTimeOffset(2026, 9, 23, 17, 0, 0, TimeSpan.Zero), result.EndUtc);
    }

    [Theory]
    [InlineData(2026, 3, 8, 23)]
    [InlineData(2026, 11, 1, 25)]
    public void HandlesNonVietnamDstDays(int year, int month, int day, int expectedHours)
    {
        var result = BusinessDateWindow.Resolve(
            new DateOnly(year, month, day), "America/New_York");

        Assert.Equal(TimeSpan.FromHours(expectedHours), result.EndUtc - result.StartUtc);
    }

    [Fact]
    public void RejectsNonIanaTimezone()
    {
        var exception = Assert.Throws<DomainRuleException>(() =>
            BusinessDateWindow.Resolve(new DateOnly(2026, 9, 23), "not/a-zone"));

        Assert.Equal("invalid-store-timezone", exception.Code);
    }
}
