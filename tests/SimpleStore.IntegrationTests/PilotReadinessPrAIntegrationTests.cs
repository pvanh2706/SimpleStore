using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SimpleStore.Application.Inventory;
using SimpleStore.Domain.Inventory;
using SimpleStore.Infrastructure.Identity;
using Xunit;

namespace SimpleStore.IntegrationTests;

public sealed class PilotReadinessPrAIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory factory;

    public PilotReadinessPrAIntegrationTests(CustomWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task CashierLifecycleForcesPasswordChangeInvalidatesSessionsAndWritesSafeAudit()
    {
        var ownerCredentials = await factory.CreateOwnerAsync();
        using var owner = factory.CreateHttpsClient();
        await owner.LoginAsync(ownerCredentials.Email, ownerCredentials.Password);
        var store = await owner.InitializeStoreAsync("PR-A accounts");

        using var createResponse = await owner.PostWithAntiforgeryAsync(
            "/api/users/cashiers",
            JsonContent.Create(new { email = $"cashier-{Guid.NewGuid():N}@example.test" }));
        createResponse.EnsureSuccessStatusCode();
        var created = (await createResponse.Content.ReadFromJsonAsync<CashierCredentialResult>())!;
        Assert.True(created.MustChangePassword);
        Assert.False(created.WasAlreadyCompleted);
        Assert.False(string.IsNullOrWhiteSpace(created.TemporaryPassword));

        using var cashier = factory.CreateHttpsClient();
        await cashier.LoginAsync(created.Email, created.TemporaryPassword);
        using (var blocked = await cashier.GetAsync("/api/products"))
        {
            Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
            Assert.Equal("password-change-required", await ReadCodeAsync(blocked));
        }

        const string changedPassword = "Changed-Test!2026";
        using (var change = await cashier.PostWithAntiforgeryAsync(
                   "/api/auth/change-password",
                   JsonContent.Create(new
                   {
                       currentPassword = created.TemporaryPassword,
                       newPassword = changedPassword
                   })))
        {
            change.EnsureSuccessStatusCode();
            using var body = JsonDocument.Parse(await change.Content.ReadAsStreamAsync());
            Assert.False(body.RootElement.GetProperty("mustChangePassword").GetBoolean());
        }
        using (var allowed = await cashier.GetAsync("/api/products"))
        {
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }
        using (var forbidden = await cashier.GetAsync("/api/users/cashiers"))
        {
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        }

        using var resetResponse = await owner.PostWithAntiforgeryAsync(
            $"/api/users/cashiers/{created.Id}/credentials/reset",
            JsonContent.Create(new { }));
        resetResponse.EnsureSuccessStatusCode();
        var reset = (await resetResponse.Content.ReadFromJsonAsync<CashierCredentialResult>())!;
        Assert.NotEqual(created.TemporaryPassword, reset.TemporaryPassword);
        using (var invalidated = await cashier.GetAsync("/api/products"))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, invalidated.StatusCode);
        }

        using var resetCashier = factory.CreateHttpsClient();
        await resetCashier.LoginAsync(reset.Email, reset.TemporaryPassword);
        using (var disable = await owner.PostWithAntiforgeryAsync(
                   $"/api/users/cashiers/{created.Id}/disable",
                   JsonContent.Create(new { })))
        {
            disable.EnsureSuccessStatusCode();
        }
        using (var disabledSession = await resetCashier.GetAsync("/api/products"))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, disabledSession.StatusCode);
        }
        using var disabledLogin = await factory.CreateHttpsClient().PostWithAntiforgeryAsync(
            "/api/auth/login",
            JsonContent.Create(new { email = reset.Email, password = reset.TemporaryPassword }));
        Assert.Equal(HttpStatusCode.Unauthorized, disabledLogin.StatusCode);
        Assert.Equal("invalid-credentials", await ReadCodeAsync(disabledLogin));

        var audit = await factory.WithDbContextAsync(async db => new
        {
            Count = await db.AccountLifecycleAudits.CountAsync(item =>
                item.StoreId == store.Id && item.TargetUserId == created.Id),
            Actions = await db.AccountLifecycleAudits
                .Where(item => item.StoreId == store.Id && item.TargetUserId == created.Id)
                .Select(item => item.Action)
                .ToArrayAsync()
        });
        Assert.Equal(4, audit.Count);
        Assert.Contains(AccountLifecycleAction.CashierCreated, audit.Actions);
        Assert.Contains(AccountLifecycleAction.CredentialChanged, audit.Actions);
        Assert.Contains(AccountLifecycleAction.CredentialReset, audit.Actions);
        Assert.Contains(AccountLifecycleAction.CashierDisabled, audit.Actions);
    }

    [Fact]
    public async Task OwnerStockAdjustmentAndStocktakeAreImmutableIdempotentAndStaleSafe()
    {
        var ownerCredentials = await factory.CreateOwnerAsync();
        using var owner = factory.CreateHttpsClient();
        await owner.LoginAsync(ownerCredentials.Email, ownerCredentials.Password);
        _ = await owner.InitializeStoreAsync("PR-A inventory");
        var product = await owner.CreateProductAsync(
            sku: $"PRA-{Guid.NewGuid():N}"[..20],
            openingQuantity: 10,
            openingCost: 20_000);

        var operationId = Guid.NewGuid();
        var request = new
        {
            operationId,
            productId = product.Id,
            quantityDelta = 2,
            adjustmentUnitCost = (decimal?)null,
            reason = "Damaged count correction"
        };
        StockAdjustmentResult adjustment;
        using (var response = await owner.PostWithAntiforgeryAsync(
                   "/api/inventory/adjustments", JsonContent.Create(request)))
        {
            response.EnsureSuccessStatusCode();
            adjustment = (await response.Content.ReadFromJsonAsync<StockAdjustmentResult>())!;
        }
        Assert.Equal(12, adjustment.QuantityAfter);
        Assert.Equal(40_000, adjustment.InventoryValueDelta);
        Assert.Equal(CostReliability.Reliable.ToString(), adjustment.CostReliability);

        using (var retry = await owner.PostWithAntiforgeryAsync(
                   "/api/inventory/adjustments", JsonContent.Create(request)))
        {
            retry.EnsureSuccessStatusCode();
            Assert.True((await retry.Content.ReadFromJsonAsync<StockAdjustmentResult>())!.WasAlreadyCompleted);
        }
        using (var changedRetry = await owner.PostWithAntiforgeryAsync(
                   "/api/inventory/adjustments",
                   JsonContent.Create(new
                   {
                       operationId,
                       productId = product.Id,
                       quantityDelta = 3,
                       adjustmentUnitCost = (decimal?)null,
                       reason = "Damaged count correction"
                   })))
        {
            Assert.Equal(HttpStatusCode.Conflict, changedRetry.StatusCode);
            Assert.Equal("operation-intent-conflict", await ReadCodeAsync(changedRetry));
        }

        var zeroContext = (await owner.GetFromJsonAsync<StocktakeContextResult>(
            $"/api/inventory/stocktakes/context/{product.Id}"))!;
        var zeroOperation = Guid.NewGuid();
        using (var zero = await owner.PostWithAntiforgeryAsync(
                   "/api/inventory/stocktakes",
                   JsonContent.Create(new
                   {
                       operationId = zeroOperation,
                       productId = product.Id,
                       expectedQuantity = zeroContext.ExpectedQuantity,
                       expectedRevision = zeroContext.ExpectedRevision,
                       countedQuantity = zeroContext.ExpectedQuantity,
                       adjustmentUnitCost = (decimal?)null,
                       note = "Matched"
                   })))
        {
            zero.EnsureSuccessStatusCode();
            var result = (await zero.Content.ReadFromJsonAsync<StocktakeResultDto>())!;
            Assert.Equal(0, result.Difference);
            Assert.Null(result.InventoryMovementId);
        }

        var staleContext = (await owner.GetFromJsonAsync<StocktakeContextResult>(
            $"/api/inventory/stocktakes/context/{product.Id}"))!;
        using (var mutate = await owner.PostWithAntiforgeryAsync(
                   "/api/inventory/adjustments",
                   JsonContent.Create(new
                   {
                       operationId = Guid.NewGuid(),
                       productId = product.Id,
                       quantityDelta = -1,
                       adjustmentUnitCost = (decimal?)null,
                       reason = "Movement during count"
                   })))
        {
            mutate.EnsureSuccessStatusCode();
        }
        using (var stale = await owner.PostWithAntiforgeryAsync(
                   "/api/inventory/stocktakes",
                   JsonContent.Create(new
                   {
                       operationId = Guid.NewGuid(),
                       productId = product.Id,
                       expectedQuantity = staleContext.ExpectedQuantity,
                       expectedRevision = staleContext.ExpectedRevision,
                       countedQuantity = 11,
                       adjustmentUnitCost = (decimal?)null,
                       note = "Stale submission"
                   })))
        {
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
            Assert.Equal("stocktake-stale", await ReadCodeAsync(stale));
        }

        var persisted = await factory.WithDbContextAsync(async db => new
        {
            Adjustments = await db.StockAdjustments.CountAsync(item => item.ProductId == product.Id),
            Stocktakes = await db.StocktakeResults.CountAsync(item => item.ProductId == product.Id),
            AdjustmentMovements = await db.InventoryMovements.CountAsync(item =>
                item.ProductId == product.Id && item.MovementType == InventoryMovementType.Adjustment),
            StocktakeMovements = await db.InventoryMovements.CountAsync(item =>
                item.ProductId == product.Id && item.MovementType == InventoryMovementType.StocktakeAdjustment)
        });
        Assert.Equal(2, persisted.Adjustments);
        Assert.Equal(1, persisted.Stocktakes);
        Assert.Equal(2, persisted.AdjustmentMovements);
        Assert.Equal(0, persisted.StocktakeMovements);

        var productStoreId = await factory.WithDbContextAsync(db => db.Products
            .Where(item => item.Id == product.Id)
            .Select(item => item.StoreId)
            .SingleAsync());
        var cashierCredentials = await factory.CreateCashierAsync(productStoreId);
        using var cashier = factory.CreateHttpsClient();
        await cashier.LoginAsync(cashierCredentials.Email, cashierCredentials.Password);
        using var forbidden = await cashier.PostWithAntiforgeryAsync(
            "/api/inventory/adjustments",
            JsonContent.Create(new
            {
                operationId = Guid.NewGuid(), productId = product.Id, quantityDelta = 1,
                adjustmentUnitCost = (decimal?)null, reason = "Forbidden"
            }));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task AccountAndInventoryOperationsAreAuthoritativelyStoreScoped()
    {
        var ownerACredentials = await factory.CreateOwnerAsync();
        var ownerBCredentials = await factory.CreateOwnerAsync();
        using var ownerA = factory.CreateHttpsClient();
        using var ownerB = factory.CreateHttpsClient();
        await ownerA.LoginAsync(ownerACredentials.Email, ownerACredentials.Password);
        await ownerB.LoginAsync(ownerBCredentials.Email, ownerBCredentials.Password);
        var storeA = await ownerA.InitializeStoreAsync("PR-A Store A");
        var storeB = await ownerB.InitializeStoreAsync("PR-A Store B");
        var productB = await ownerB.CreateProductAsync(openingQuantity: 5, openingCost: 10);

        CashierCredentialResult cashierB;
        using (var createB = await ownerB.PostWithAntiforgeryAsync(
                   "/api/users/cashiers",
                   JsonContent.Create(new { email = $"cashier-b-{Guid.NewGuid():N}@example.test" })))
        {
            createB.EnsureSuccessStatusCode();
            cashierB = (await createB.Content.ReadFromJsonAsync<CashierCredentialResult>())!;
        }
        using (var crossStoreDisable = await ownerA.PostWithAntiforgeryAsync(
                   $"/api/users/cashiers/{cashierB.Id}/disable", JsonContent.Create(new { })))
        {
            Assert.Equal(HttpStatusCode.NotFound, crossStoreDisable.StatusCode);
        }
        using (var createA = await ownerA.PostWithAntiforgeryAsync(
                   "/api/users/cashiers",
                   JsonContent.Create(new
                   {
                       email = $"cashier-a-{Guid.NewGuid():N}@example.test",
                       storeId = storeB.Id,
                       role = ApplicationRoles.Owner
                   })))
        {
            createA.EnsureSuccessStatusCode();
            var cashierA = (await createA.Content.ReadFromJsonAsync<CashierCredentialResult>())!;
            var authoritativeStore = await factory.WithDbContextAsync(db => db.Users
                .Where(item => item.Id == cashierA.Id)
                .Select(item => item.StoreId)
                .SingleAsync());
            Assert.Equal(storeA.Id, authoritativeStore);
        }
        var listedA = (await ownerA.GetFromJsonAsync<CashierAccountResult[]>("/api/users/cashiers"))!;
        Assert.DoesNotContain(listedA, item => item.Id == cashierB.Id);

        using (var crossStoreContext = await ownerA.GetAsync(
                   $"/api/inventory/stocktakes/context/{productB.Id}"))
        {
            Assert.Equal(HttpStatusCode.NotFound, crossStoreContext.StatusCode);
        }
        using (var crossStoreAdjustment = await ownerA.PostWithAntiforgeryAsync(
                   "/api/inventory/adjustments",
                   JsonContent.Create(new
                   {
                       operationId = Guid.NewGuid(), productId = productB.Id,
                       quantityDelta = -1, adjustmentUnitCost = (decimal?)null,
                       reason = "Must not cross Store boundary"
                   })))
        {
            Assert.Equal(HttpStatusCode.NotFound, crossStoreAdjustment.StatusCode);
        }
    }

    [Fact]
    public async Task NonZeroStocktakeCreatesExactlyOneTypedMovementAndExactRetryDoesNotDuplicate()
    {
        var credentials = await factory.CreateOwnerAsync();
        using var owner = factory.CreateHttpsClient();
        await owner.LoginAsync(credentials.Email, credentials.Password);
        _ = await owner.InitializeStoreAsync("PR-A stocktake movement");
        var product = await owner.CreateProductAsync(openingQuantity: 10, openingCost: 20);
        var context = (await owner.GetFromJsonAsync<StocktakeContextResult>(
            $"/api/inventory/stocktakes/context/{product.Id}"))!;
        var operationId = Guid.NewGuid();
        var command = new
        {
            operationId,
            productId = product.Id,
            expectedQuantity = context.ExpectedQuantity,
            expectedRevision = context.ExpectedRevision,
            countedQuantity = 8,
            adjustmentUnitCost = (decimal?)null,
            note = "Physical count"
        };

        StocktakeResultDto first;
        using (var response = await owner.PostWithAntiforgeryAsync(
                   "/api/inventory/stocktakes", JsonContent.Create(command)))
        {
            response.EnsureSuccessStatusCode();
            first = (await response.Content.ReadFromJsonAsync<StocktakeResultDto>())!;
        }
        Assert.Equal(-2, first.Difference);
        Assert.Equal(-40, first.InventoryValueDelta);
        Assert.Equal(CostReliability.Reliable.ToString(), first.CostReliability);
        Assert.NotNull(first.InventoryMovementId);

        using (var retry = await owner.PostWithAntiforgeryAsync(
                   "/api/inventory/stocktakes", JsonContent.Create(command)))
        {
            retry.EnsureSuccessStatusCode();
            var retried = (await retry.Content.ReadFromJsonAsync<StocktakeResultDto>())!;
            Assert.True(retried.WasAlreadyCompleted);
            Assert.Equal(first.Id, retried.Id);
        }

        var state = await factory.WithDbContextAsync(async db => new
        {
            Sources = await db.StocktakeResults.CountAsync(item => item.ProductId == product.Id),
            Movements = await db.InventoryMovements.CountAsync(item =>
                item.ProductId == product.Id && item.MovementType == InventoryMovementType.StocktakeAdjustment),
            Movement = await db.InventoryMovements.SingleAsync(item =>
                item.ProductId == product.Id && item.MovementType == InventoryMovementType.StocktakeAdjustment),
            Quantity = await db.InventoryBalances.Where(item => item.ProductId == product.Id)
                .Select(item => item.QuantityOnHand).SingleAsync()
        });
        Assert.Equal(1, state.Sources);
        Assert.Equal(1, state.Movements);
        Assert.Equal(8, state.Quantity);
        Assert.Equal(10, state.Movement.StocktakeExpectedQuantity);
        Assert.Equal(8, state.Movement.StocktakeCountedQuantity);
        Assert.Equal("Physical count", state.Movement.Reason);
    }

    [Fact]
    public async Task ConcurrentAdjustmentsSerializeOnTheBalanceWithoutLosingEitherMovement()
    {
        var credentials = await factory.CreateOwnerAsync();
        using var setup = factory.CreateHttpsClient();
        await setup.LoginAsync(credentials.Email, credentials.Password);
        _ = await setup.InitializeStoreAsync("PR-A adjustment concurrency");
        var product = await setup.CreateProductAsync(openingQuantity: 10, openingCost: 20);
        using var clientA = factory.CreateHttpsClient();
        using var clientB = factory.CreateHttpsClient();
        await clientA.LoginAsync(credentials.Email, credentials.Password);
        await clientB.LoginAsync(credentials.Email, credentials.Password);

        var responses = await Task.WhenAll(
            clientA.PostWithAntiforgeryAsync(
                "/api/inventory/adjustments",
                JsonContent.Create(new
                {
                    operationId = Guid.NewGuid(), productId = product.Id, quantityDelta = 1,
                    adjustmentUnitCost = (decimal?)null, reason = "Concurrent A"
                })),
            clientB.PostWithAntiforgeryAsync(
                "/api/inventory/adjustments",
                JsonContent.Create(new
                {
                    operationId = Guid.NewGuid(), productId = product.Id, quantityDelta = 1,
                    adjustmentUnitCost = (decimal?)null, reason = "Concurrent B"
                })));
        using var responseA = responses[0];
        using var responseB = responses[1];
        responseA.EnsureSuccessStatusCode();
        responseB.EnsureSuccessStatusCode();

        var state = await factory.WithDbContextAsync(async db => new
        {
            Quantity = await db.InventoryBalances.Where(item => item.ProductId == product.Id)
                .Select(item => item.QuantityOnHand).SingleAsync(),
            Sources = await db.StockAdjustments.CountAsync(item => item.ProductId == product.Id),
            Movements = await db.InventoryMovements.CountAsync(item =>
                item.ProductId == product.Id && item.MovementType == InventoryMovementType.Adjustment)
        });
        Assert.Equal(12, state.Quantity);
        Assert.Equal(2, state.Sources);
        Assert.Equal(2, state.Movements);
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        return body.RootElement.GetProperty("code").GetString();
    }
}

public sealed class OwnerBootstrapIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory factory;

    public OwnerBootstrapIntegrationTests(CustomWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task BootstrapCreatesOnlyTheFirstOwnerAndExactRetryIsSafe()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var bootstrap = scope.ServiceProvider.GetRequiredService<OwnerBootstrapService>();
        const string email = "first-owner@example.test";
        const string password = "Bootstrap-Test!2026";

        var created = await bootstrap.BootstrapAsync(email, password, CancellationToken.None);
        var retried = await bootstrap.BootstrapAsync(" FIRST-OWNER@example.test ", password, CancellationToken.None);
        var rejected = await bootstrap.BootstrapAsync(
            "second-owner@example.test", password, CancellationToken.None);

        Assert.True(created.Success);
        Assert.False(created.WasAlreadyBootstrapped);
        Assert.True(retried.Success);
        Assert.True(retried.WasAlreadyBootstrapped);
        Assert.False(rejected.Success);
        Assert.False(rejected.WasAlreadyBootstrapped);
        var owner = await factory.WithDbContextAsync(db => db.Users.SingleAsync());
        Assert.Null(owner.StoreId);

        using var client = factory.CreateHttpsClient();
        await client.LoginAsync(email, password);
        using var response = await client.PostWithAntiforgeryAsync(
            "/api/bootstrap-owner",
            JsonContent.Create(new { email, password }));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
