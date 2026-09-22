using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF Core generates inline column arrays for composite indexes and keys.

namespace SimpleStore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementSlice5Stage5ADebtBackend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Stores",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "Asia/Ho_Chi_Minh");

            migrationBuilder.CreateTable(
                name: "DebtPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebtPayments", x => x.Id);
                    table.CheckConstraint("CK_DebtPayments_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_DebtPayments_PartyPurposeDirection", "([Purpose] = 'CustomerDebtCollection' AND [Direction] = 'MoneyIn' AND [CustomerId] IS NOT NULL AND [SupplierId] IS NULL) OR ([Purpose] = 'SupplierDebtSettlement' AND [Direction] = 'MoneyOut' AND [SupplierId] IS NOT NULL AND [CustomerId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_DebtPayments_AspNetUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DebtPayments_Customers_StoreId_CustomerId",
                        columns: x => new { x.StoreId, x.CustomerId },
                        principalTable: "Customers",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DebtPayments_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DebtPayments_Suppliers_StoreId_SupplierId",
                        columns: x => new { x.StoreId, x.SupplierId },
                        principalTable: "Suppliers",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DebtPayments_OperationId",
                table: "DebtPayments",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DebtPayments_PerformedByUserId",
                table: "DebtPayments",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DebtPayments_StoreId_CustomerId_OccurredAt",
                table: "DebtPayments",
                columns: new[] { "StoreId", "CustomerId", "OccurredAt" },
                filter: "[CustomerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DebtPayments_StoreId_Purpose_OccurredAt",
                table: "DebtPayments",
                columns: new[] { "StoreId", "Purpose", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DebtPayments_StoreId_SupplierId_OccurredAt",
                table: "DebtPayments",
                columns: new[] { "StoreId", "SupplierId", "OccurredAt" },
                filter: "[SupplierId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DebtPayments");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Stores");
        }
    }
}
