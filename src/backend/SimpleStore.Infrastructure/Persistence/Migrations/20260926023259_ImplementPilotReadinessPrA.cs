using System;
using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable CA1861 // EF migration API requires inline column-name arrays.

#nullable disable

namespace SimpleStore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementPilotReadinessPrA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CostReliability",
                table: "InventoryMovements",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "InventoryMovements",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StocktakeCountedQuantity",
                table: "InventoryMovements",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StocktakeExpectedQuantity",
                table: "InventoryMovements",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DisabledAt",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PasswordChangeRequiredAt",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PasswordChangedAt",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccountLifecycleAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountLifecycleAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountLifecycleAudits_AspNetUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountLifecycleAudits_AspNetUsers_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountLifecycleAudits_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityDelta = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    AdjustmentUnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    EffectiveUnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CostReliability = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    InventoryValueDelta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    QuantityBefore = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    QuantityAfter = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    InventoryValueBefore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InventoryValueAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AverageCostAfter = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    HasAverageCostAfter = table.Column<bool>(type: "bit", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    InventoryMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustments", x => x.Id);
                    table.UniqueConstraint("AK_StockAdjustments_StoreId_Id", x => new { x.StoreId, x.Id });
                    table.ForeignKey(
                        name: "FK_StockAdjustments_AspNetUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAdjustments_InventoryMovements_InventoryMovementId",
                        column: x => x.InventoryMovementId,
                        principalTable: "InventoryMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAdjustments_Products_StoreId_ProductId",
                        columns: x => new { x.StoreId, x.ProductId },
                        principalTable: "Products",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAdjustments_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAdjustments_Warehouses_StoreId_WarehouseId",
                        columns: x => new { x.StoreId, x.WarehouseId },
                        principalTable: "Warehouses",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StocktakeResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ExpectedBalanceRowVersion = table.Column<byte[]>(type: "varbinary(8)", maxLength: 8, nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Difference = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    AdjustmentUnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    EffectiveUnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CostReliability = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    InventoryValueDelta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InventoryValueBefore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InventoryValueAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AverageCostAfter = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    HasAverageCostAfter = table.Column<bool>(type: "bit", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    InventoryMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StocktakeResults", x => x.Id);
                    table.UniqueConstraint("AK_StocktakeResults_StoreId_Id", x => new { x.StoreId, x.Id });
                    table.ForeignKey(
                        name: "FK_StocktakeResults_AspNetUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StocktakeResults_InventoryMovements_InventoryMovementId",
                        column: x => x.InventoryMovementId,
                        principalTable: "InventoryMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StocktakeResults_Products_StoreId_ProductId",
                        columns: x => new { x.StoreId, x.ProductId },
                        principalTable: "Products",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StocktakeResults_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StocktakeResults_Warehouses_StoreId_WarehouseId",
                        columns: x => new { x.StoreId, x.WarehouseId },
                        principalTable: "Warehouses",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountLifecycleAudits_PerformedByUserId",
                table: "AccountLifecycleAudits",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountLifecycleAudits_StoreId_TargetUserId_OccurredAt",
                table: "AccountLifecycleAudits",
                columns: new[] { "StoreId", "TargetUserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountLifecycleAudits_TargetUserId",
                table: "AccountLifecycleAudits",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_InventoryMovementId",
                table: "StockAdjustments",
                column: "InventoryMovementId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_PerformedByUserId",
                table: "StockAdjustments",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_StoreId_OperationId",
                table: "StockAdjustments",
                columns: new[] { "StoreId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_StoreId_ProductId_OccurredAt",
                table: "StockAdjustments",
                columns: new[] { "StoreId", "ProductId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_StoreId_WarehouseId",
                table: "StockAdjustments",
                columns: new[] { "StoreId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeResults_InventoryMovementId",
                table: "StocktakeResults",
                column: "InventoryMovementId",
                unique: true,
                filter: "[InventoryMovementId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeResults_PerformedByUserId",
                table: "StocktakeResults",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeResults_StoreId_OperationId",
                table: "StocktakeResults",
                columns: new[] { "StoreId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeResults_StoreId_ProductId_OccurredAt",
                table: "StocktakeResults",
                columns: new[] { "StoreId", "ProductId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeResults_StoreId_WarehouseId",
                table: "StocktakeResults",
                columns: new[] { "StoreId", "WarehouseId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountLifecycleAudits");

            migrationBuilder.DropTable(
                name: "StockAdjustments");

            migrationBuilder.DropTable(
                name: "StocktakeResults");

            migrationBuilder.DropColumn(
                name: "CostReliability",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "StocktakeCountedQuantity",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "StocktakeExpectedQuantity",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "DisabledAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PasswordChangeRequiredAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PasswordChangedAt",
                table: "AspNetUsers");
        }
    }
}

#pragma warning restore CA1861
