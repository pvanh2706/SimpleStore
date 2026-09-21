using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Application.ProductImports;
using SimpleStore.Application.Products;
using Xunit;

namespace SimpleStore.IntegrationTests;

public sealed class Slice1IntegrationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task ProtectedProductEndpointReturnsUnauthorizedWithoutSession()
    {
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InitializeStoreCreatesOneMainWarehouseAndRetryDoesNotDuplicate()
    {
        var (client, _) = await CreateAuthenticatedStoreAsync();
        using (client)
        {
            var firstStore = await client.GetFromJsonAsync<SimpleStore.Application.Stores.StoreResult>(
                "/api/store/current");
            var retriedStore = await client.InitializeStoreAsync("Tên gửi lại không tạo store mới");

            Assert.NotNull(firstStore);
            Assert.Equal(firstStore.Id, retriedStore.Id);
            Assert.Equal(firstStore.MainWarehouseId, retriedStore.MainWarehouseId);

            var counts = await factory.WithDbContextAsync(async dbContext => new
            {
                Stores = await dbContext.Stores.CountAsync(store => store.Id == firstStore.Id),
                Warehouses = await dbContext.Warehouses.CountAsync(
                    warehouse => warehouse.StoreId == firstStore.Id),
                MainWarehouses = await dbContext.Warehouses.CountAsync(
                    warehouse => warehouse.StoreId == firstStore.Id && warehouse.IsMain)
            });

            Assert.Equal(1, counts.Stores);
            Assert.Equal(1, counts.Warehouses);
            Assert.Equal(1, counts.MainWarehouses);
        }
    }

    [Fact]
    public async Task CreateProductWithOpeningStockCreatesMovementAndCorrectBalance()
    {
        var (client, _) = await CreateAuthenticatedStoreAsync();
        using (client)
        {
            var product = await client.CreateProductAsync(
                sku: null,
                barcode: "893000000001",
                name: "Nước suối",
                openingQuantity: 20,
                openingCost: 8_000);

            Assert.StartsWith("SP-", product.Sku, StringComparison.Ordinal);
            Assert.Equal(20, product.QuantityOnHand);
            Assert.Equal(160_000, product.InventoryValue);
            Assert.Equal(8_000, product.AverageCost);

            var movements = await client.GetFromJsonAsync<InventoryMovementResult[]>(
                $"/api/products/{product.Id}/movements");
            Assert.NotNull(movements);
            var movement = Assert.Single(movements);
            Assert.Equal("OpeningBalance", movement.Type);
            Assert.Equal(20, movement.QuantityDelta);
            Assert.Equal(160_000, movement.InventoryValueDelta);

            var persisted = await factory.WithDbContextAsync(async dbContext => new
            {
                Products = await dbContext.Products.CountAsync(productEntity => productEntity.Id == product.Id),
                Balances = await dbContext.InventoryBalances.CountAsync(
                    balance => balance.ProductId == product.Id),
                Movements = await dbContext.InventoryMovements.CountAsync(
                    inventoryMovement => inventoryMovement.ProductId == product.Id)
            });
            Assert.Equal(1, persisted.Products);
            Assert.Equal(1, persisted.Balances);
            Assert.Equal(1, persisted.Movements);
        }
    }

    [Fact]
    public async Task DuplicateIdentifiersAreRejectedWithoutPartialInventoryState()
    {
        var (client, _) = await CreateAuthenticatedStoreAsync();
        using (client)
        {
            _ = await client.CreateProductAsync("SKU-DUP", "BAR-DUP", openingQuantity: 5, openingCost: 100);
            var beforeCounts = await GetInventoryCountsAsync();

            using var duplicateSkuResponse = await CreateProductRawAsync(
                client,
                "sku-dup",
                "BAR-OTHER",
                3,
                100);
            using var duplicateBarcodeResponse = await CreateProductRawAsync(
                client,
                "SKU-OTHER",
                "bar-dup",
                3,
                100);

            Assert.Equal(HttpStatusCode.Conflict, duplicateSkuResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, duplicateBarcodeResponse.StatusCode);
            var afterCounts = await GetInventoryCountsAsync();
            Assert.Equal(beforeCounts, afterCounts);
        }

        async Task<(int Products, int Balances, int Movements)> GetInventoryCountsAsync() =>
            await factory.WithDbContextAsync(async dbContext =>
                (
                    await dbContext.Products.CountAsync(),
                    await dbContext.InventoryBalances.CountAsync(),
                    await dbContext.InventoryMovements.CountAsync()
                ));
    }

    [Fact]
    public async Task SameSkuIsAllowedAcrossStoresAndCrossStoreProductIsHidden()
    {
        var (clientA, _) = await CreateAuthenticatedStoreAsync("Cửa hàng A");
        var (clientB, _) = await CreateAuthenticatedStoreAsync("Cửa hàng B");
        using (clientA)
        using (clientB)
        {
            var productA = await clientA.CreateProductAsync("SHARED-SKU", "BAR-A", "Sản phẩm A");
            var productB = await clientB.CreateProductAsync("SHARED-SKU", "BAR-B", "Sản phẩm B");

            Assert.NotEqual(productA.Id, productB.Id);
            var crossStoreResponse = await clientA.GetAsync($"/api/products/{productB.Id}");
            Assert.Equal(HttpStatusCode.NotFound, crossStoreResponse.StatusCode);
        }
    }

    [Fact]
    public async Task ProductCanBeUpdatedAndDeactivatedWithoutDeletion()
    {
        var (client, _) = await CreateAuthenticatedStoreAsync();
        using (client)
        {
            var product = await client.CreateProductAsync("SKU-EDIT", "BAR-EDIT", "Tên cũ");
            using var updateResponse = await client.PutWithAntiforgeryAsync(
                $"/api/products/{product.Id}",
                JsonContent.Create(new
                {
                    sku = "SKU-EDITED",
                    barcode = "BAR-EDITED",
                    name = "Tên mới",
                    unit = "chai",
                    salePrice = 15_000,
                    referencePurchaseCost = 9_000
                }));
            updateResponse.EnsureSuccessStatusCode();

            using var deactivateResponse = await client.PostWithAntiforgeryAsync(
                $"/api/products/{product.Id}/deactivate",
                JsonContent.Create(new { }));
            Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

            var updated = await client.GetFromJsonAsync<ProductResult>($"/api/products/{product.Id}");
            Assert.NotNull(updated);
            Assert.Equal("Tên mới", updated.Name);
            Assert.False(updated.IsActive);

            var databaseProduct = await factory.WithDbContextAsync(dbContext =>
                dbContext.Products.AsNoTracking().SingleAsync(item => item.Id == product.Id));
            Assert.False(databaseProduct.IsActive);
        }
    }

    [Fact]
    public async Task ProductListSupportsSearchActiveFilterAndPaging()
    {
        var (client, _) = await CreateAuthenticatedStoreAsync();
        using (client)
        {
            _ = await client.CreateProductAsync("COCA-1", "1001", "Coca lon");
            _ = await client.CreateProductAsync("WATER-1", "1002", "Nước suối");
            _ = await client.CreateProductAsync("COCA-2", "1003", "Coca chai");

            var searchResult = await client.GetFromJsonAsync<ProductListResult>(
                "/api/products?search=Coca&isActive=true&page=1&pageSize=1");

            Assert.NotNull(searchResult);
            Assert.Single(searchResult.Items);
            Assert.Equal(2, searchResult.TotalCount);
            Assert.Equal(2, searchResult.TotalPages);
        }
    }

    [Fact]
    public async Task ImportValidationReportsRowsAndDoesNotCreateProducts()
    {
        var (client, store) = await CreateAuthenticatedStoreAsync();
        using (client)
        {
            const string csv = "SKU,Barcode,Name,Unit,SalePrice,OpeningCost,OpeningQuantity\n"
                + "DUP,111,Sản phẩm 1,cái,10000,,2\n"
                + "DUP,111,,cái,invalid,5000,1\n";

            var result = await ValidateImportAsync(client, csv);

            Assert.False(result.IsValid);
            Assert.Null(result.ImportId);
            Assert.Contains(result.Errors, error => error.Code == "opening-cost-required" && error.RowNumber == 2);
            Assert.Contains(result.Errors, error => error.Code == "invalid-number" && error.RowNumber == 3);
            Assert.Contains(result.Errors, error => error.Code == "duplicate-sku-in-file");
            Assert.Contains(result.Errors, error => error.Code == "duplicate-barcode-in-file");
            var productCount = await factory.WithDbContextAsync(dbContext =>
                dbContext.Products.CountAsync(product => product.StoreId == store.Id));
            Assert.Equal(0, productCount);
        }
    }

    [Fact]
    public async Task ImportConfirmIsAllOrNothingWhenPreviewBecomesStale()
    {
        var (client, store) = await CreateAuthenticatedStoreAsync();
        using (client)
        {
            const string csv = "SKU,Barcode,Name,Unit,SalePrice,OpeningCost,OpeningQuantity\n"
                + "IMPORT-A,IMP-A,Sản phẩm A,cái,10000,5000,2\n"
                + "IMPORT-B,IMP-B,Sản phẩm B,cái,12000,6000,3\n";
            var validation = await ValidateImportAsync(client, csv);
            Assert.True(validation.IsValid);
            Assert.NotNull(validation.ImportId);

            _ = await client.CreateProductAsync("IMPORT-B", "OTHER-B", "Sản phẩm chen ngang");
            using var confirmResponse = await client.PostWithAntiforgeryAsync(
                $"/api/product-imports/{validation.ImportId}/confirm",
                JsonContent.Create(new { }));

            Assert.Equal(HttpStatusCode.BadRequest, confirmResponse.StatusCode);
            var importedAExists = await factory.WithDbContextAsync(dbContext =>
                dbContext.Products.AnyAsync(product =>
                    product.StoreId == store.Id && product.NormalizedSku == "IMPORT-A"));
            Assert.False(importedAExists);
        }
    }

    [Fact]
    public async Task ImportConfirmCreatesInventoryAndRetryDoesNotDuplicate()
    {
        var (client, store) = await CreateAuthenticatedStoreAsync();
        using (client)
        {
            const string csv = "SKU,Barcode,Name,Unit,SalePrice,OpeningCost,OpeningQuantity\n"
                + "IMPORT-1,9001,Sản phẩm import 1,cái,10000,4000,5\n"
                + ",9002,Sản phẩm import 2,chai,12000,,0\n";
            var validation = await ValidateImportAsync(client, csv);
            Assert.True(validation.IsValid);
            Assert.NotNull(validation.ImportId);
            Assert.StartsWith("SP-", validation.Rows[1].Sku, StringComparison.Ordinal);

            var firstConfirm = await ConfirmImportAsync(client, validation.ImportId.Value);
            var retryConfirm = await ConfirmImportAsync(client, validation.ImportId.Value);

            Assert.Equal(2, firstConfirm.ImportedProductCount);
            Assert.False(firstConfirm.WasAlreadyCompleted);
            Assert.True(retryConfirm.WasAlreadyCompleted);

            var counts = await factory.WithDbContextAsync(async dbContext => new
            {
                Products = await dbContext.Products.CountAsync(product => product.StoreId == store.Id),
                Balances = await dbContext.InventoryBalances.CountAsync(balance => balance.StoreId == store.Id),
                Movements = await dbContext.InventoryMovements.CountAsync(movement => movement.StoreId == store.Id),
                Quantity = await dbContext.InventoryBalances
                    .Where(balance => balance.StoreId == store.Id)
                    .SumAsync(balance => balance.QuantityOnHand),
                Value = await dbContext.InventoryBalances
                    .Where(balance => balance.StoreId == store.Id)
                    .SumAsync(balance => balance.InventoryValue)
            });
            Assert.Equal(2, counts.Products);
            Assert.Equal(2, counts.Balances);
            Assert.Equal(1, counts.Movements);
            Assert.Equal(5, counts.Quantity);
            Assert.Equal(20_000, counts.Value);
        }
    }

    private async Task<(HttpClient Client, SimpleStore.Application.Stores.StoreResult Store)>
        CreateAuthenticatedStoreAsync(string storeName = "Cửa hàng kiểm thử")
    {
        var credentials = await factory.CreateOwnerAsync();
        var client = factory.CreateHttpsClient();
        await client.LoginAsync(credentials.Email, credentials.Password);
        var store = await client.InitializeStoreAsync(storeName);
        return (client, store);
    }

    private static Task<HttpResponseMessage> CreateProductRawAsync(
        HttpClient client,
        string sku,
        string barcode,
        decimal openingQuantity,
        decimal openingCost) =>
        client.PostWithAntiforgeryAsync(
            "/api/products",
            JsonContent.Create(new
            {
                sku,
                barcode,
                name = "Sản phẩm trùng",
                unit = "cái",
                salePrice = 1000,
                referencePurchaseCost = openingCost,
                openingQuantity,
                openingCost
            }));

    private static async Task<ProductImportValidationResult> ValidateImportAsync(
        HttpClient client,
        string csv)
    {
        using var form = new MultipartFormDataContent();
        form.Add(
            new ByteArrayContent(Encoding.UTF8.GetBytes(csv)),
            "file",
            "products.csv");
        using var response = await client.PostWithAntiforgeryAsync(
            "/api/product-imports/validate",
            form);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductImportValidationResult>())!;
    }

    private static async Task<ProductImportConfirmResult> ConfirmImportAsync(
        HttpClient client,
        Guid importId)
    {
        using var response = await client.PostWithAntiforgeryAsync(
            $"/api/product-imports/{importId}/confirm",
            JsonContent.Create(new { }));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductImportConfirmResult>())!;
    }
}
