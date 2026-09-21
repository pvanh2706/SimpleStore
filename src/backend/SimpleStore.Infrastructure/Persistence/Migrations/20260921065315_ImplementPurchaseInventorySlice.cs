using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF Core generates inline column arrays for composite indexes and keys.

namespace SimpleStore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementPurchaseInventorySlice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessOperations",
                columns: table => new
                {
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ResultReference = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessOperations", x => x.OperationId);
                    table.ForeignKey(
                        name: "FK_BusinessOperations_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.UniqueConstraint("AK_Suppliers_StoreId_Id", x => new { x.StoreId, x.Id });
                    table.ForeignKey(
                        name: "FK_Suppliers_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchases", x => x.Id);
                    table.UniqueConstraint("AK_Purchases_StoreId_Id", x => new { x.StoreId, x.Id });
                    table.CheckConstraint("CK_Purchases_TotalAmount", "[TotalAmount] >= 0");
                    table.ForeignKey(
                        name: "FK_Purchases_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Purchases_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Purchases_Suppliers_StoreId_SupplierId",
                        columns: x => new { x.StoreId, x.SupplierId },
                        principalTable: "Suppliers",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseLines", x => x.Id);
                    table.CheckConstraint("CK_PurchaseLines_LineAmount", "[LineAmount] >= 0");
                    table.CheckConstraint("CK_PurchaseLines_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_PurchaseLines_UnitPrice", "[UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_PurchaseLines_Products_StoreId_ProductId",
                        columns: x => new { x.StoreId, x.ProductId },
                        principalTable: "Products",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseLines_Purchases_StoreId_PurchaseId",
                        columns: x => new { x.StoreId, x.PurchaseId },
                        principalTable: "Purchases",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchasePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchasePayments", x => x.Id);
                    table.CheckConstraint("CK_PurchasePayments_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_PurchasePayments_AspNetUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchasePayments_Purchases_StoreId_PurchaseId",
                        columns: x => new { x.StoreId, x.PurchaseId },
                        principalTable: "Purchases",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOperations_StoreId_OperationId",
                table: "BusinessOperations",
                columns: new[] { "StoreId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOperations_StoreId_OperationType_CreatedAt",
                table: "BusinessOperations",
                columns: new[] { "StoreId", "OperationType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseLines_PurchaseId_ProductId",
                table: "PurchaseLines",
                columns: new[] { "PurchaseId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseLines_StoreId_ProductId",
                table: "PurchaseLines",
                columns: new[] { "StoreId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseLines_StoreId_PurchaseId",
                table: "PurchaseLines",
                columns: new[] { "StoreId", "PurchaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePayments_PerformedByUserId",
                table: "PurchasePayments",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePayments_StoreId_PurchaseId",
                table: "PurchasePayments",
                columns: new[] { "StoreId", "PurchaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_CreatedByUserId",
                table: "Purchases",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_StoreId_Status_CreatedAt",
                table: "Purchases",
                columns: new[] { "StoreId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_StoreId_SupplierId_Status",
                table: "Purchases",
                columns: new[] { "StoreId", "SupplierId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_StoreId_Name",
                table: "Suppliers",
                columns: new[] { "StoreId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_StoreId_Phone",
                table: "Suppliers",
                columns: new[] { "StoreId", "Phone" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessOperations");

            migrationBuilder.DropTable(
                name: "PurchaseLines");

            migrationBuilder.DropTable(
                name: "PurchasePayments");

            migrationBuilder.DropTable(
                name: "Purchases");

            migrationBuilder.DropTable(
                name: "Suppliers");
        }
    }
}
