using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF Core generates inline column arrays for composite indexes and keys.

namespace SimpleStore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementSlice4BackendCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_StoreId_WarehouseId",
                table: "InventoryMovements");

            migrationBuilder.AddColumn<long>(
                name: "ReferencePurchaseCostRevision",
                table: "Products",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LedgerSequence",
                table: "InventoryMovements",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_SaleLines_StoreId_Id",
                table: "SaleLines",
                columns: new[] { "StoreId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_PurchaseLines_StoreId_Id",
                table: "PurchaseLines",
                columns: new[] { "StoreId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_InventoryMovements_StoreId_Id",
                table: "InventoryMovements",
                columns: new[] { "StoreId", "Id" });

            migrationBuilder.CreateTable(
                name: "PurchaseLineReversalBases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityBefore = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    InventoryValueBefore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AverageCostBefore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    HasAverageCostBefore = table.Column<bool>(type: "bit", nullable: false),
                    ReferencePurchaseCostBefore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ReferencePurchaseCostApplied = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReferencePurchaseCostRevisionAfterPurchase = table.Column<long>(type: "bigint", nullable: false),
                    PurchaseMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseLineReversalBases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseLineReversalBases_InventoryMovements_StoreId_PurchaseMovementId",
                        columns: x => new { x.StoreId, x.PurchaseMovementId },
                        principalTable: "InventoryMovements",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseLineReversalBases_Products_StoreId_ProductId",
                        columns: x => new { x.StoreId, x.ProductId },
                        principalTable: "Products",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseLineReversalBases_PurchaseLines_StoreId_PurchaseLineId",
                        columns: x => new { x.StoreId, x.PurchaseLineId },
                        principalTable: "PurchaseLines",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseLineReversalBases_Purchases_StoreId_PurchaseId",
                        columns: x => new { x.StoreId, x.PurchaseId },
                        principalTable: "Purchases",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseLineReversalBases_Warehouses_StoreId_WarehouseId",
                        columns: x => new { x.StoreId, x.WarehouseId },
                        principalTable: "Warehouses",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseVoids",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalPurchaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    VoidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseVoids", x => x.Id);
                    table.UniqueConstraint("AK_PurchaseVoids_StoreId_Id", x => new { x.StoreId, x.Id });
                    table.ForeignKey(
                        name: "FK_PurchaseVoids_AspNetUsers_VoidedByUserId",
                        column: x => x.VoidedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseVoids_Purchases_StoreId_OriginalPurchaseId",
                        columns: x => new { x.StoreId, x.OriginalPurchaseId },
                        principalTable: "Purchases",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseVoids_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Returns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalSaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TotalReturnAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CompletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Returns", x => x.Id);
                    table.UniqueConstraint("AK_Returns_StoreId_Id", x => new { x.StoreId, x.Id });
                    table.CheckConstraint("CK_Returns_RefundAmount", "[RefundAmount] >= 0 AND [RefundAmount] <= [TotalReturnAmount]");
                    table.CheckConstraint("CK_Returns_TotalReturnAmount", "[TotalReturnAmount] >= 0");
                    table.ForeignKey(
                        name: "FK_Returns_AspNetUsers_CompletedByUserId",
                        column: x => x.CompletedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Returns_Sales_StoreId_OriginalSaleId",
                        columns: x => new { x.StoreId, x.OriginalSaleId },
                        principalTable: "Sales",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Returns_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleVoids",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalSaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    VoidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleVoids", x => x.Id);
                    table.UniqueConstraint("AK_SaleVoids_StoreId_Id", x => new { x.StoreId, x.Id });
                    table.ForeignKey(
                        name: "FK_SaleVoids_AspNetUsers_VoidedByUserId",
                        column: x => x.VoidedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleVoids_Sales_StoreId_OriginalSaleId",
                        columns: x => new { x.StoreId, x.OriginalSaleId },
                        principalTable: "Sales",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleVoids_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReturnLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalSaleLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Restock = table.Column<bool>(type: "bit", nullable: false),
                    UnitSalePriceBasis = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReturnLineAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitCostBasis = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RestockedInventoryValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnLines", x => x.Id);
                    table.UniqueConstraint("AK_ReturnLines_StoreId_Id", x => new { x.StoreId, x.Id });
                    table.CheckConstraint("CK_ReturnLines_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_ReturnLines_Values", "[UnitSalePriceBasis] >= 0 AND [ReturnLineAmount] >= 0 AND [UnitCostBasis] >= 0 AND [RestockedInventoryValue] >= 0");
                    table.ForeignKey(
                        name: "FK_ReturnLines_Products_StoreId_ProductId",
                        columns: x => new { x.StoreId, x.ProductId },
                        principalTable: "Products",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnLines_Returns_StoreId_ReturnId",
                        columns: x => new { x.StoreId, x.ReturnId },
                        principalTable: "Returns",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnLines_SaleLines_StoreId_OriginalSaleLineId",
                        columns: x => new { x.StoreId, x.OriginalSaleLineId },
                        principalTable: "SaleLines",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReturnRefundPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnRefundPayments", x => x.Id);
                    table.CheckConstraint("CK_ReturnRefundPayments_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_ReturnRefundPayments_AspNetUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnRefundPayments_Returns_StoreId_ReturnId",
                        columns: x => new { x.StoreId, x.ReturnId },
                        principalTable: "Returns",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_StoreId_WarehouseId_ProductId_LedgerSequence",
                table: "InventoryMovements",
                columns: new[] { "StoreId", "WarehouseId", "ProductId", "LedgerSequence" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseLineReversalBases_PurchaseLineId",
                table: "PurchaseLineReversalBases",
                column: "PurchaseLineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseLineReversalBases_StoreId_ProductId",
                table: "PurchaseLineReversalBases",
                columns: new[] { "StoreId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseLineReversalBases_StoreId_PurchaseId",
                table: "PurchaseLineReversalBases",
                columns: new[] { "StoreId", "PurchaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseLineReversalBases_StoreId_PurchaseLineId",
                table: "PurchaseLineReversalBases",
                columns: new[] { "StoreId", "PurchaseLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseLineReversalBases_StoreId_PurchaseMovementId",
                table: "PurchaseLineReversalBases",
                columns: new[] { "StoreId", "PurchaseMovementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseLineReversalBases_StoreId_WarehouseId",
                table: "PurchaseLineReversalBases",
                columns: new[] { "StoreId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseVoids_OriginalPurchaseId",
                table: "PurchaseVoids",
                column: "OriginalPurchaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseVoids_StoreId_OriginalPurchaseId",
                table: "PurchaseVoids",
                columns: new[] { "StoreId", "OriginalPurchaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseVoids_VoidedByUserId",
                table: "PurchaseVoids",
                column: "VoidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnLines_ReturnId_OriginalSaleLineId",
                table: "ReturnLines",
                columns: new[] { "ReturnId", "OriginalSaleLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnLines_StoreId_OriginalSaleLineId",
                table: "ReturnLines",
                columns: new[] { "StoreId", "OriginalSaleLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnLines_StoreId_ProductId",
                table: "ReturnLines",
                columns: new[] { "StoreId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnLines_StoreId_ReturnId",
                table: "ReturnLines",
                columns: new[] { "StoreId", "ReturnId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRefundPayments_PerformedByUserId",
                table: "ReturnRefundPayments",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRefundPayments_StoreId_ReturnId",
                table: "ReturnRefundPayments",
                columns: new[] { "StoreId", "ReturnId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Returns_CompletedByUserId",
                table: "Returns",
                column: "CompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Returns_StoreId_OriginalSaleId_CompletedAt",
                table: "Returns",
                columns: new[] { "StoreId", "OriginalSaleId", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleVoids_OriginalSaleId",
                table: "SaleVoids",
                column: "OriginalSaleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleVoids_StoreId_OriginalSaleId",
                table: "SaleVoids",
                columns: new[] { "StoreId", "OriginalSaleId" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleVoids_VoidedByUserId",
                table: "SaleVoids",
                column: "VoidedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseLineReversalBases");

            migrationBuilder.DropTable(
                name: "PurchaseVoids");

            migrationBuilder.DropTable(
                name: "ReturnLines");

            migrationBuilder.DropTable(
                name: "ReturnRefundPayments");

            migrationBuilder.DropTable(
                name: "SaleVoids");

            migrationBuilder.DropTable(
                name: "Returns");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_SaleLines_StoreId_Id",
                table: "SaleLines");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PurchaseLines_StoreId_Id",
                table: "PurchaseLines");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_InventoryMovements_StoreId_Id",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_StoreId_WarehouseId_ProductId_LedgerSequence",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "ReferencePurchaseCostRevision",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "LedgerSequence",
                table: "InventoryMovements");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_StoreId_WarehouseId",
                table: "InventoryMovements",
                columns: new[] { "StoreId", "WarehouseId" });
        }
    }
}
