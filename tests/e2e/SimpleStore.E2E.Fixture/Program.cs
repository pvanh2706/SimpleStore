using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Domain.Corrections;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Purchases;
using SimpleStore.Domain.Returns;
using SimpleStore.Domain.Sales;
using SimpleStore.Domain.Stores;
using SimpleStore.Infrastructure.Persistence;

var command = args.FirstOrDefault()
    ?? throw new InvalidOperationException("Expected seed or snapshot command.");
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SimpleStore")
    ?? throw new InvalidOperationException("ConnectionStrings__SimpleStore is required.");
var ownerEmail = Environment.GetEnvironmentVariable("SIMPLESTORE_E2E_EMAIL")
    ?? throw new InvalidOperationException("SIMPLESTORE_E2E_EMAIL is required.");
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer(connectionString)
    .Options;
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
await using var db = new ApplicationDbContext(options);

var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
var storeId = owner.StoreId
    ?? throw new InvalidOperationException("The E2E owner has not initialized a Store.");

if (command.Equals("seed", StringComparison.OrdinalIgnoreCase))
{
    await SeedAsync(db, storeId, owner.Id);
    Console.WriteLine(JsonSerializer.Serialize(new { seeded = true }, jsonOptions));
    return;
}

if (command.Equals("snapshot", StringComparison.OrdinalIgnoreCase))
{
    var eventCounts = await db.C14ExperimentEvents
        .Where(item => item.StoreId == storeId)
        .GroupBy(item => item.EventType)
        .Select(group => new { EventType = group.Key, Count = group.Count() })
        .ToDictionaryAsync(item => item.EventType, item => item.Count);
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        todayOpened = eventCounts.GetValueOrDefault("TodayOpened"),
        signalShown = eventCounts.GetValueOrDefault("SignalShown"),
        whyOpened = eventCounts.GetValueOrDefault("WhyOpened"),
        purchaseDraftStarted = eventCounts.GetValueOrDefault("PurchaseDraftStarted"),
        purchaseCount = await db.Purchases.CountAsync(item => item.StoreId == storeId),
    }, jsonOptions));
    return;
}

throw new InvalidOperationException($"Unknown fixture command '{command}'.");

static async Task SeedAsync(ApplicationDbContext db, Guid storeId, Guid actorUserId)
{
    const string skuPrefix = "S6E2E-";
    if (await db.Products.AnyAsync(product =>
            product.StoreId == storeId && product.Sku.StartsWith(skuPrefix)))
    {
        return;
    }

    var store = await db.Stores.SingleAsync(item => item.Id == storeId);
    var warehouse = await db.Warehouses.SingleAsync(item =>
        item.StoreId == storeId && item.IsMain);
    var currentWindow = BusinessDateWindow.Resolve(
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById(store.TimeZoneId)).DateTime),
        store.TimeZoneId);
    var velocityStart = BusinessDateWindow.Resolve(
        currentWindow.BusinessDate.AddDays(-7), store.TimeZoneId).StartUtc;
    var velocityEnd = currentWindow.StartUtc;

    await db.Stores.Where(item => item.Id == storeId)
        .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CreatedAt, velocityStart));

    var risk = CreateProduct(storeId, $"{skuPrefix}RISK", "Alpha risk with evidence", velocityStart);
    var outOfStock = CreateProduct(storeId, $"{skuPrefix}OUT", "Beta out of stock", velocityStart);
    var negative = CreateProduct(storeId, $"{skuPrefix}NEG", "Charlie negative stock", velocityStart);
    var secondRisk = CreateProduct(storeId, $"{skuPrefix}RISK2", "Delta second risk", velocityStart);
    var partial = CreateProduct(storeId, $"{skuPrefix}PART", "Echo partial out", velocityStart.AddTicks(1));
    var inactive = CreateProduct(storeId, $"{skuPrefix}OFF", "Hidden inactive signal", velocityStart);
    inactive.Deactivate(velocityEnd.AddTicks(-1));
    db.Products.AddRange(risk, outOfStock, negative, secondRisk, partial, inactive);

    db.InventoryBalances.AddRange(
        CreateBalance(storeId, warehouse.Id, risk.Id, 2, velocityEnd),
        CreateBalance(storeId, warehouse.Id, outOfStock.Id, 0, velocityEnd),
        CreateBalance(storeId, warehouse.Id, negative.Id, -1, velocityEnd),
        CreateBalance(storeId, warehouse.Id, secondRisk.Id, 1, velocityEnd),
        CreateBalance(storeId, warehouse.Id, partial.Id, 0, velocityEnd),
        CreateBalance(storeId, warehouse.Id, inactive.Id, -1, velocityEnd));

    var riskSale = CreateSale(storeId, warehouse.Id, actorUserId, risk, 10,
        velocityStart.AddDays(1));
    var riskVoidedSale = CreateSale(storeId, warehouse.Id, actorUserId, risk, 2,
        velocityStart.AddTicks(-1));
    var outSale = CreateSale(storeId, warehouse.Id, actorUserId, outOfStock, 3,
        velocityStart.AddDays(3));
    var negativeSale = CreateSale(storeId, warehouse.Id, actorUserId, negative, 2,
        velocityStart.AddDays(4));
    var secondRiskSale = CreateSale(storeId, warehouse.Id, actorUserId, secondRisk, 4,
        velocityStart.AddDays(5));
    var partialSale = CreateSale(storeId, warehouse.Id, actorUserId, partial, 1,
        velocityStart.AddDays(2));
    var inactiveSale = CreateSale(storeId, warehouse.Id, actorUserId, inactive, 2,
        velocityStart.AddDays(1));
    db.Sales.AddRange(
        riskSale, riskVoidedSale, outSale, negativeSale, secondRiskSale, partialSale, inactiveSale);

    var riskLine = riskSale.Lines.Single();
    db.Returns.Add(CustomerReturn.Complete(
        storeId,
        riskSale.Id,
        actorUserId,
        [new ReturnLineInput(
            riskLine.Id,
            risk.Id,
            1,
            false,
            riskLine.UnitSalePrice,
            riskLine.UnitSalePrice,
            riskLine.UnitCostAtSale,
            0)],
        0,
        null,
        velocityStart.AddDays(2)));
    db.SaleVoids.Add(SaleVoid.Create(
        storeId,
        riskVoidedSale.Id,
        "E2E velocity correction",
        actorUserId,
        velocityStart.AddDays(3)));

    await db.SaveChangesAsync();
}

static Product CreateProduct(
    Guid storeId,
    string sku,
    string name,
    DateTimeOffset createdAt) =>
    Product.Create(storeId, sku, null, name, "unit", 12_000, 8_000, createdAt);

static InventoryBalance CreateBalance(
    Guid storeId,
    Guid warehouseId,
    Guid productId,
    decimal quantity,
    DateTimeOffset updatedAt)
{
    var balance = InventoryBalance.Create(
        storeId, warehouseId, productId, OpeningInventory.Create(0, null), updatedAt);
    balance.RestoreExact(quantity, quantity > 0 ? quantity * 8_000 : 0, 8_000, quantity > 0, updatedAt);
    return balance;
}

static Sale CreateSale(
    Guid storeId,
    Guid warehouseId,
    Guid actorUserId,
    Product product,
    decimal quantity,
    DateTimeOffset completedAt)
{
    var total = quantity * product.SalePrice;
    return Sale.Complete(
        storeId,
        warehouseId,
        null,
        actorUserId,
        [new SaleLineInput(
            product.Id,
            product.Name,
            product.Sku,
            product.Unit,
            quantity,
            product.SalePrice,
            product.ReferencePurchaseCost ?? 0,
            CostReliability.Reliable)],
        [new SalePaymentInput(total, PaymentMethod.Cash)],
        completedAt);
}
