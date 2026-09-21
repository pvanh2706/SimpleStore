using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF Core generates inline column arrays for composite indexes and keys.

namespace SimpleStore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenSlice1DataIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryBalances_Products_ProductId",
                table: "InventoryBalances");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryBalances_Warehouses_WarehouseId",
                table: "InventoryBalances");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovements_Products_ProductId",
                table: "InventoryMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovements_Warehouses_WarehouseId",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_ProductId",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_WarehouseId",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_InventoryBalances_ProductId",
                table: "InventoryBalances");

            migrationBuilder.DropIndex(
                name: "IX_InventoryBalances_WarehouseId",
                table: "InventoryBalances");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Warehouses_StoreId_Id",
                table: "Warehouses",
                columns: new[] { "StoreId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Products_StoreId_Id",
                table: "Products",
                columns: new[] { "StoreId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImports_CreatedByUserId",
                table: "ProductImports",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_PerformedByUserId",
                table: "InventoryMovements",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_StoreId_WarehouseId",
                table: "InventoryMovements",
                columns: new[] { "StoreId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalances_StoreId_ProductId",
                table: "InventoryBalances",
                columns: new[] { "StoreId", "ProductId" });

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryBalances_Products_StoreId_ProductId",
                table: "InventoryBalances",
                columns: new[] { "StoreId", "ProductId" },
                principalTable: "Products",
                principalColumns: new[] { "StoreId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryBalances_Warehouses_StoreId_WarehouseId",
                table: "InventoryBalances",
                columns: new[] { "StoreId", "WarehouseId" },
                principalTable: "Warehouses",
                principalColumns: new[] { "StoreId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovements_AspNetUsers_PerformedByUserId",
                table: "InventoryMovements",
                column: "PerformedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovements_Products_StoreId_ProductId",
                table: "InventoryMovements",
                columns: new[] { "StoreId", "ProductId" },
                principalTable: "Products",
                principalColumns: new[] { "StoreId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovements_Warehouses_StoreId_WarehouseId",
                table: "InventoryMovements",
                columns: new[] { "StoreId", "WarehouseId" },
                principalTable: "Warehouses",
                principalColumns: new[] { "StoreId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductImports_AspNetUsers_CreatedByUserId",
                table: "ProductImports",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Stores_AspNetUsers_OwnerUserId",
                table: "Stores",
                column: "OwnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryBalances_Products_StoreId_ProductId",
                table: "InventoryBalances");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryBalances_Warehouses_StoreId_WarehouseId",
                table: "InventoryBalances");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovements_AspNetUsers_PerformedByUserId",
                table: "InventoryMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovements_Products_StoreId_ProductId",
                table: "InventoryMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovements_Warehouses_StoreId_WarehouseId",
                table: "InventoryMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductImports_AspNetUsers_CreatedByUserId",
                table: "ProductImports");

            migrationBuilder.DropForeignKey(
                name: "FK_Stores_AspNetUsers_OwnerUserId",
                table: "Stores");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Warehouses_StoreId_Id",
                table: "Warehouses");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Products_StoreId_Id",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductImports_CreatedByUserId",
                table: "ProductImports");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_PerformedByUserId",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_StoreId_WarehouseId",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_InventoryBalances_StoreId_ProductId",
                table: "InventoryBalances");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_ProductId",
                table: "InventoryMovements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_WarehouseId",
                table: "InventoryMovements",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalances_ProductId",
                table: "InventoryBalances",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalances_WarehouseId",
                table: "InventoryBalances",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryBalances_Products_ProductId",
                table: "InventoryBalances",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryBalances_Warehouses_WarehouseId",
                table: "InventoryBalances",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovements_Products_ProductId",
                table: "InventoryMovements",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovements_Warehouses_WarehouseId",
                table: "InventoryMovements",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

#pragma warning restore CA1861
