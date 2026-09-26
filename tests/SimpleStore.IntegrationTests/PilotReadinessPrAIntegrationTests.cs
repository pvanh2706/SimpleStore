using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
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
        var createdState = await GetCredentialStateAsync(
            created.Id,
            created.TemporaryPassword,
            "not-the-password");
        Assert.True(createdState.IsEnabled);
        Assert.True(createdState.MustChangePassword);
        Assert.True(createdState.MatchesFirstPassword);
        Assert.Equal(1, await CountAuditAsync(created.Id, AccountLifecycleAction.CashierCreated));

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
        var changedState = await GetCredentialStateAsync(
            created.Id,
            created.TemporaryPassword,
            changedPassword);
        Assert.True(changedState.IsEnabled);
        Assert.False(changedState.MustChangePassword);
        Assert.False(changedState.MatchesFirstPassword);
        Assert.True(changedState.MatchesSecondPassword);
        Assert.NotNull(changedState.PasswordChangedAt);
        Assert.NotEqual(createdState.SecurityStamp, changedState.SecurityStamp);
        Assert.Equal(1, await CountAuditAsync(created.Id, AccountLifecycleAction.CredentialChanged));

        using var resetResponse = await owner.PostWithAntiforgeryAsync(
            $"/api/users/cashiers/{created.Id}/credentials/reset",
            JsonContent.Create(new { }));
        resetResponse.EnsureSuccessStatusCode();
        var reset = (await resetResponse.Content.ReadFromJsonAsync<CashierCredentialResult>())!;
        Assert.NotEqual(created.TemporaryPassword, reset.TemporaryPassword);
        var resetState = await GetCredentialStateAsync(created.Id, changedPassword, reset.TemporaryPassword);
        Assert.True(resetState.IsEnabled);
        Assert.True(resetState.MustChangePassword);
        Assert.False(resetState.MatchesFirstPassword);
        Assert.True(resetState.MatchesSecondPassword);
        Assert.NotNull(resetState.PasswordChangeRequiredAt);
        Assert.NotEqual(changedState.SecurityStamp, resetState.SecurityStamp);
        Assert.Equal(1, await CountAuditAsync(created.Id, AccountLifecycleAction.CredentialReset));
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
        var disabledState = await GetCredentialStateAsync(created.Id, reset.TemporaryPassword, changedPassword);
        Assert.False(disabledState.IsEnabled);
        Assert.NotNull(disabledState.DisabledAt);
        Assert.NotEqual(resetState.SecurityStamp, disabledState.SecurityStamp);
        Assert.Equal(1, await CountAuditAsync(created.Id, AccountLifecycleAction.CashierDisabled));

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
    public async Task FailedLifecycleAuditInsertsRollBackCredentialsStateAndSecurityStamp()
    {
        var ownerCredentials = await factory.CreateOwnerAsync();
        using var owner = factory.CreateHttpsClient();
        await owner.LoginAsync(ownerCredentials.Email, ownerCredentials.Password);
        _ = await owner.InitializeStoreAsync("PR-A account rollback");
        using var create = await owner.PostWithAntiforgeryAsync(
            "/api/users/cashiers",
            JsonContent.Create(new { email = $"rollback-{Guid.NewGuid():N}@example.test" }));
        create.EnsureSuccessStatusCode();
        var cashier = (await create.Content.ReadFromJsonAsync<CashierCredentialResult>())!;
        var before = await GetCredentialStateAsync(
            cashier.Id,
            cashier.TemporaryPassword,
            "not-the-password");
        using var activeCashier = factory.CreateHttpsClient();
        await activeCashier.LoginAsync(cashier.Email, cashier.TemporaryPassword);

        const string rejectedPassword = "Must-Rollback!2026";
        await CreateRejectAuditTriggerAsync(AccountLifecycleAction.CredentialChanged);
        try
        {
            using var response = await activeCashier.PostWithAntiforgeryAsync(
                "/api/auth/change-password",
                JsonContent.Create(new
                {
                    currentPassword = cashier.TemporaryPassword,
                    newPassword = rejectedPassword
                }));
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
        finally
        {
            await DropRejectAuditTriggerAsync();
        }
        var afterFailedChange = await GetCredentialStateAsync(
            cashier.Id,
            cashier.TemporaryPassword,
            rejectedPassword);
        Assert.True(afterFailedChange.MustChangePassword);
        Assert.True(afterFailedChange.MatchesFirstPassword);
        Assert.False(afterFailedChange.MatchesSecondPassword);
        Assert.Equal(before.SecurityStamp, afterFailedChange.SecurityStamp);
        Assert.Equal(0, await CountAuditAsync(cashier.Id, AccountLifecycleAction.CredentialChanged));

        await CreateRejectAuditTriggerAsync(AccountLifecycleAction.CredentialReset);
        try
        {
            using var response = await owner.PostWithAntiforgeryAsync(
                $"/api/users/cashiers/{cashier.Id}/credentials/reset",
                JsonContent.Create(new { }));
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
        finally
        {
            await DropRejectAuditTriggerAsync();
        }
        var afterFailedReset = await GetCredentialStateAsync(
            cashier.Id,
            cashier.TemporaryPassword,
            rejectedPassword);
        Assert.True(afterFailedReset.MustChangePassword);
        Assert.True(afterFailedReset.MatchesFirstPassword);
        Assert.False(afterFailedReset.MatchesSecondPassword);
        Assert.Equal(before.SecurityStamp, afterFailedReset.SecurityStamp);
        Assert.Equal(0, await CountAuditAsync(cashier.Id, AccountLifecycleAction.CredentialReset));

        await CreateRejectAuditTriggerAsync(AccountLifecycleAction.CashierDisabled);
        try
        {
            using var response = await owner.PostWithAntiforgeryAsync(
                $"/api/users/cashiers/{cashier.Id}/disable",
                JsonContent.Create(new { }));
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
        finally
        {
            await DropRejectAuditTriggerAsync();
        }

        var after = await GetCredentialStateAsync(
            cashier.Id,
            cashier.TemporaryPassword,
            "not-the-password");
        Assert.True(after.IsEnabled);
        Assert.Null(after.DisabledAt);
        Assert.Equal(before.SecurityStamp, after.SecurityStamp);
        Assert.True(after.MatchesFirstPassword);
        Assert.Equal(0, await CountAuditAsync(cashier.Id, AccountLifecycleAction.CashierDisabled));
        using var stillActive = await activeCashier.GetAsync("/api/auth/session");
        stillActive.EnsureSuccessStatusCode();
        using var session = JsonDocument.Parse(await stillActive.Content.ReadAsStreamAsync());
        Assert.True(session.RootElement.GetProperty("isAuthenticated").GetBoolean());
    }

    [Fact]
    public async Task InvalidPrAQuantityCostAndNegativeCountAreRejectedWithoutPersistence()
    {
        var credentials = await factory.CreateOwnerAsync();
        using var owner = factory.CreateHttpsClient();
        await owner.LoginAsync(credentials.Email, credentials.Password);
        _ = await owner.InitializeStoreAsync("PR-A precision");
        var product = await owner.CreateProductAsync(openingQuantity: 0);
        var context = (await owner.GetFromJsonAsync<StocktakeContextResult>(
            $"/api/inventory/stocktakes/context/{product.Id}"))!;
        var operationIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        using (var quantity = await owner.PostWithAntiforgeryAsync(
                   "/api/inventory/adjustments",
                   JsonContent.Create(new
                   {
                       operationId = operationIds[0], productId = product.Id,
                       quantityDelta = 0.0004m, adjustmentUnitCost = 1m, reason = "Too precise"
                   })))
        {
            Assert.Equal(HttpStatusCode.BadRequest, quantity.StatusCode);
            Assert.Equal("invalid-adjustment-quantity-precision", await ReadCodeAsync(quantity));
        }
        using (var cost = await owner.PostWithAntiforgeryAsync(
                   "/api/inventory/adjustments",
                   JsonContent.Create(new
                   {
                       operationId = operationIds[1], productId = product.Id,
                       quantityDelta = 1m, adjustmentUnitCost = 12.34567m, reason = "Cost too precise"
                   })))
        {
            Assert.Equal(HttpStatusCode.BadRequest, cost.StatusCode);
            Assert.Equal("invalid-adjustment-unit-cost-precision", await ReadCodeAsync(cost));
        }
        using (var negativeCount = await owner.PostWithAntiforgeryAsync(
                   "/api/inventory/stocktakes",
                   JsonContent.Create(new
                   {
                       operationId = operationIds[2], productId = product.Id,
                       expectedQuantity = context.ExpectedQuantity, expectedRevision = context.ExpectedRevision,
                       countedQuantity = -1m, adjustmentUnitCost = (decimal?)null, note = "Invalid negative"
                   })))
        {
            Assert.Equal(HttpStatusCode.BadRequest, negativeCount.StatusCode);
            Assert.Equal("invalid-stocktake-counted-quantity", await ReadCodeAsync(negativeCount));
        }
        using (var preciseCount = await owner.PostWithAntiforgeryAsync(
                   "/api/inventory/stocktakes",
                   JsonContent.Create(new
                   {
                       operationId = operationIds[3], productId = product.Id,
                       expectedQuantity = context.ExpectedQuantity, expectedRevision = context.ExpectedRevision,
                       countedQuantity = 0.0004m, adjustmentUnitCost = 1m, note = "Invalid precision"
                   })))
        {
            Assert.Equal(HttpStatusCode.BadRequest, preciseCount.StatusCode);
            Assert.Equal("invalid-stocktake-counted-quantity-precision", await ReadCodeAsync(preciseCount));
        }

        var state = await factory.WithDbContextAsync(async db => new
        {
            Quantity = await db.InventoryBalances.Where(item => item.ProductId == product.Id)
                .Select(item => item.QuantityOnHand).SingleAsync(),
            Value = await db.InventoryBalances.Where(item => item.ProductId == product.Id)
                .Select(item => item.InventoryValue).SingleAsync(),
            Adjustments = await db.StockAdjustments.CountAsync(item => item.ProductId == product.Id),
            Stocktakes = await db.StocktakeResults.CountAsync(item => item.ProductId == product.Id),
            Movements = await db.InventoryMovements.CountAsync(item =>
                item.ProductId == product.Id
                && (item.MovementType == InventoryMovementType.Adjustment
                    || item.MovementType == InventoryMovementType.StocktakeAdjustment)),
            Operations = await db.BusinessOperations.CountAsync(item => operationIds.Contains(item.OperationId))
        });
        Assert.Equal(0, state.Quantity);
        Assert.Equal(0, state.Value);
        Assert.Equal(0, state.Adjustments);
        Assert.Equal(0, state.Stocktakes);
        Assert.Equal(0, state.Movements);
        Assert.Equal(0, state.Operations);
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

    private async Task<int> CountAuditAsync(Guid userId, AccountLifecycleAction action) =>
        await factory.WithDbContextAsync(db => db.AccountLifecycleAudits.CountAsync(item =>
            item.TargetUserId == userId && item.Action == action));

    private async Task<CredentialState> GetCredentialStateAsync(
        Guid userId,
        string firstPassword,
        string secondPassword)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId.ToString());
        Assert.NotNull(user);
        return new CredentialState(
            user.IsEnabled,
            user.MustChangePassword,
            user.DisabledAt,
            user.PasswordChangeRequiredAt,
            user.PasswordChangedAt,
            user.SecurityStamp!,
            await userManager.CheckPasswordAsync(user, firstPassword),
            await userManager.CheckPasswordAsync(user, secondPassword));
    }

    private Task<int> CreateRejectAuditTriggerAsync(AccountLifecycleAction action)
    {
        var sql = action switch
        {
            AccountLifecycleAction.CredentialChanged => """
                CREATE TRIGGER [TR_Test_RejectLifecycleAudit]
                ON [AccountLifecycleAudits]
                AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM [inserted] WHERE [Action] = N'CredentialChanged')
                        THROW 51001, 'Intentional lifecycle audit failure.', 1;
                END
                """,
            AccountLifecycleAction.CredentialReset => """
                CREATE TRIGGER [TR_Test_RejectLifecycleAudit]
                ON [AccountLifecycleAudits]
                AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM [inserted] WHERE [Action] = N'CredentialReset')
                        THROW 51001, 'Intentional lifecycle audit failure.', 1;
                END
                """,
            AccountLifecycleAction.CashierDisabled => """
                CREATE TRIGGER [TR_Test_RejectLifecycleAudit]
                ON [AccountLifecycleAudits]
                AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM [inserted] WHERE [Action] = N'CashierDisabled')
                        THROW 51001, 'Intentional lifecycle audit failure.', 1;
                END
                """,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
        return factory.WithDbContextAsync(db => db.Database.ExecuteSqlRawAsync(sql));
    }

    private Task<int> DropRejectAuditTriggerAsync() =>
        factory.WithDbContextAsync(db => db.Database.ExecuteSqlRawAsync(
            "DROP TRIGGER IF EXISTS [TR_Test_RejectLifecycleAudit]"));

    private sealed record CredentialState(
        bool IsEnabled,
        bool MustChangePassword,
        DateTimeOffset? DisabledAt,
        DateTimeOffset? PasswordChangeRequiredAt,
        DateTimeOffset? PasswordChangedAt,
        string SecurityStamp,
        bool MatchesFirstPassword,
        bool MatchesSecondPassword);
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
