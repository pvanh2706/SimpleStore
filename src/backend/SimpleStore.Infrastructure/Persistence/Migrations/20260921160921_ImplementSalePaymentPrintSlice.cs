using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF Core generates inline column arrays for composite indexes and keys.

namespace SimpleStore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementSalePaymentPrintSlice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowNegativeStock",
                table: "Stores",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasAverageCost",
                table: "InventoryBalances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE [InventoryBalances]
                SET [HasAverageCost] = 1
                WHERE [QuantityOnHand] > 0;
                """);

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.UniqueConstraint("AK_Customers_StoreId_Id", x => new { x.StoreId, x.Id });
                    table.ForeignKey(
                        name: "FK_Customers_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NegativeStockSettingAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldValue = table.Column<bool>(type: "bit", nullable: false),
                    NewValue = table.Column<bool>(type: "bit", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NegativeStockSettingAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NegativeStockSettingAudits_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NegativeStockSettingAudits_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales", x => x.Id);
                    table.UniqueConstraint("AK_Sales_StoreId_Id", x => new { x.StoreId, x.Id });
                    table.CheckConstraint("CK_Sales_TotalAmount", "[TotalAmount] >= 0");
                    table.ForeignKey(
                        name: "FK_Sales_AspNetUsers_CompletedByUserId",
                        column: x => x.CompletedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sales_Customers_StoreId_CustomerId",
                        columns: x => new { x.StoreId, x.CustomerId },
                        principalTable: "Customers",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sales_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sales_Warehouses_StoreId_WarehouseId",
                        columns: x => new { x.StoreId, x.WarehouseId },
                        principalTable: "Warehouses",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ProductSku = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProductUnit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitSalePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitCostAtSale = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CostReliability = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleLines", x => x.Id);
                    table.CheckConstraint("CK_SaleLines_LineAmount", "[LineAmount] >= 0");
                    table.CheckConstraint("CK_SaleLines_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_SaleLines_UnitCostAtSale", "[UnitCostAtSale] >= 0");
                    table.CheckConstraint("CK_SaleLines_UnitSalePrice", "[UnitSalePrice] >= 0");
                    table.ForeignKey(
                        name: "FK_SaleLines_Products_StoreId_ProductId",
                        columns: x => new { x.StoreId, x.ProductId },
                        principalTable: "Products",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleLines_Sales_StoreId_SaleId",
                        columns: x => new { x.StoreId, x.SaleId },
                        principalTable: "Sales",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalePayments", x => x.Id);
                    table.CheckConstraint("CK_SalePayments_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_SalePayments_AspNetUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalePayments_Sales_StoreId_SaleId",
                        columns: x => new { x.StoreId, x.SaleId },
                        principalTable: "Sales",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_StoreId_Name",
                table: "Customers",
                columns: new[] { "StoreId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_StoreId_Phone",
                table: "Customers",
                columns: new[] { "StoreId", "Phone" });

            migrationBuilder.CreateIndex(
                name: "IX_NegativeStockSettingAudits_ChangedByUserId",
                table: "NegativeStockSettingAudits",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NegativeStockSettingAudits_StoreId_ChangedAt",
                table: "NegativeStockSettingAudits",
                columns: new[] { "StoreId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleLines_SaleId_ProductId",
                table: "SaleLines",
                columns: new[] { "SaleId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleLines_StoreId_ProductId",
                table: "SaleLines",
                columns: new[] { "StoreId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleLines_StoreId_SaleId",
                table: "SaleLines",
                columns: new[] { "StoreId", "SaleId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalePayments_PerformedByUserId",
                table: "SalePayments",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalePayments_StoreId_SaleId",
                table: "SalePayments",
                columns: new[] { "StoreId", "SaleId" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CompletedByUserId",
                table: "Sales",
                column: "CompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_StoreId_CompletedAt",
                table: "Sales",
                columns: new[] { "StoreId", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_StoreId_CustomerId_CompletedAt",
                table: "Sales",
                columns: new[] { "StoreId", "CustomerId", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_StoreId_WarehouseId",
                table: "Sales",
                columns: new[] { "StoreId", "WarehouseId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NegativeStockSettingAudits");

            migrationBuilder.DropTable(
                name: "SaleLines");

            migrationBuilder.DropTable(
                name: "SalePayments");

            migrationBuilder.DropTable(
                name: "Sales");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropColumn(
                name: "AllowNegativeStock",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "HasAverageCost",
                table: "InventoryBalances");
        }
    }
}

#pragma warning restore CA1861
