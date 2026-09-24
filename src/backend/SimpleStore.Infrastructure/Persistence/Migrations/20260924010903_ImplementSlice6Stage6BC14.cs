using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // EF Core generates constant arrays for migration metadata.

namespace SimpleStore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementSlice6Stage6BC14 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "C14ExperimentEvents",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttentionKind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_C14ExperimentEvents", x => x.EventId);
                    table.CheckConstraint("CK_C14ExperimentEvents_Identity", "([EventType] = 'TodayOpened' AND [ProductId] IS NULL AND [AttentionKind] IS NULL) OR ([EventType] IN ('SignalShown', 'WhyOpened', 'PurchaseDraftStarted') AND [ProductId] IS NOT NULL AND [AttentionKind] IN ('NegativeStock', 'OutOfStock', 'LowStockRisk'))");
                    table.ForeignKey(
                        name: "FK_C14ExperimentEvents_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C14ExperimentEvents_Products_StoreId_ProductId",
                        columns: x => new { x.StoreId, x.ProductId },
                        principalTable: "Products",
                        principalColumns: new[] { "StoreId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C14ExperimentEvents_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_C14ExperimentEvents_ActorUserId",
                table: "C14ExperimentEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_C14ExperimentEvents_StoreId_EventType_OccurredAt",
                table: "C14ExperimentEvents",
                columns: new[] { "StoreId", "EventType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_C14ExperimentEvents_StoreId_OccurredAt",
                table: "C14ExperimentEvents",
                columns: new[] { "StoreId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_C14ExperimentEvents_StoreId_ProductId",
                table: "C14ExperimentEvents",
                columns: new[] { "StoreId", "ProductId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "C14ExperimentEvents");
        }
    }
}
#pragma warning restore CA1861
