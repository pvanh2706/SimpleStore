using SimpleStore.Domain;
using SimpleStore.Domain.Products;
using Xunit;

namespace SimpleStore.Domain.Tests.Products;

public sealed class ProductTests
{
    [Theory]
    [InlineData("", "Name", "cái", 10, "sku-required")]
    [InlineData("SKU-1", "", "cái", 10, "name-required")]
    [InlineData("SKU-1", "Name", "", 10, "unit-required")]
    [InlineData("SKU-1", "Name", "cái", -1, "invalid-sale-price")]
    public void CreateRejectsInvalidProduct(
        string sku,
        string name,
        string unit,
        decimal salePrice,
        string expectedCode)
    {
        var exception = Assert.Throws<DomainRuleException>(() => Product.Create(
            Guid.NewGuid(),
            sku,
            null,
            name,
            unit,
            salePrice,
            null,
            DateTimeOffset.UtcNow));

        Assert.Equal(expectedCode, exception.Code);
    }

    [Fact]
    public void CreateNormalizesIdentifiersWithinProductBoundary()
    {
        var product = Product.Create(
            Guid.NewGuid(),
            "  sku-01 ",
            "  893123  ",
            "Nước ngọt",
            "chai",
            12_000,
            8_000,
            DateTimeOffset.UtcNow);

        Assert.Equal("sku-01", product.Sku);
        Assert.Equal("SKU-01", product.NormalizedSku);
        Assert.Equal("893123", product.Barcode);
        Assert.Equal("893123", product.NormalizedBarcode);
    }

    [Fact]
    public void DeactivatePreservesProductAndMarksItInactive()
    {
        var product = Product.Create(
            Guid.NewGuid(),
            "SKU-01",
            null,
            "Nước ngọt",
            "chai",
            12_000,
            null,
            DateTimeOffset.UtcNow);
        var updatedAt = DateTimeOffset.UtcNow.AddMinutes(1);

        product.Deactivate(updatedAt);

        Assert.False(product.IsActive);
        Assert.Equal(updatedAt, product.UpdatedAt);
    }

    [Fact]
    public void ReferencePurchaseCostRevisionChangesOnlyWhenCostValueChanges()
    {
        var product = Product.Create(
            Guid.NewGuid(), "SKU-01", null, "Product", "item", 100, 10, DateTimeOffset.UtcNow);
        var initialRevision = product.ReferencePurchaseCostRevision;

        product.UpdateReferencePurchaseCost(10, DateTimeOffset.UtcNow);
        Assert.Equal(initialRevision, product.ReferencePurchaseCostRevision);

        product.Update("SKU-01", null, "Renamed", "item", 120, 10, DateTimeOffset.UtcNow);
        Assert.Equal(initialRevision, product.ReferencePurchaseCostRevision);

        product.UpdateReferencePurchaseCost(20, DateTimeOffset.UtcNow);
        Assert.Equal(initialRevision + 1, product.ReferencePurchaseCostRevision);

        product.RestoreReferencePurchaseCost(10, DateTimeOffset.UtcNow);
        Assert.Equal(initialRevision + 2, product.ReferencePurchaseCostRevision);
    }

    [Fact]
    public void ReferencePurchaseCostRevisionDistinguishesNullAndKnownZero()
    {
        var product = Product.Create(
            Guid.NewGuid(), "SKU-NULL", null, "Product", "item", 100, null, DateTimeOffset.UtcNow);
        var initialRevision = product.ReferencePurchaseCostRevision;

        product.UpdateReferencePurchaseCost(0, DateTimeOffset.UtcNow);
        Assert.Equal(initialRevision + 1, product.ReferencePurchaseCostRevision);

        product.RestoreReferencePurchaseCost(null, DateTimeOffset.UtcNow);
        Assert.Equal(initialRevision + 2, product.ReferencePurchaseCostRevision);
    }
}
