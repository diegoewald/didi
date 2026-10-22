using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalErp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SequenceNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestForQuotationAwardId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupplierId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupplierNameSnapshot = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IssuedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IssuedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancellationReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_RequestForQuotationAwards_RequestForQuotationAwardId",
                        column: x => x.RequestForQuotationAwardId,
                        principalTable: "RequestForQuotationAwards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Users_CancelledByUserId",
                        column: x => x.CancelledByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Users_IssuedByUserId",
                        column: x => x.IssuedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestForQuotationAwardLineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DescriptionSnapshot = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    MeasurementUnitId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeasurementUnitNameSnapshot = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UnitPrice = table.Column<long>(type: "INTEGER", nullable: false),
                    PromisedDeliveryDate = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_MeasurementUnits_MeasurementUnitId",
                        column: x => x.MeasurementUnitId,
                        principalTable: "MeasurementUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_RequestForQuotationAwardLines_RequestForQuotationAwardLineId",
                        column: x => x.RequestForQuotationAwardLineId,
                        principalTable: "RequestForQuotationAwardLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_MeasurementUnitId",
                table: "PurchaseOrderLines",
                column: "MeasurementUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_PurchaseOrderId_LineNumber",
                table: "PurchaseOrderLines",
                columns: new[] { "PurchaseOrderId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_RequestForQuotationAwardLineId",
                table: "PurchaseOrderLines",
                column: "RequestForQuotationAwardLineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CancelledByUserId",
                table: "PurchaseOrders",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CreatedAtUtc",
                table: "PurchaseOrders",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CreatedByUserId",
                table: "PurchaseOrders",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_IssuedByUserId",
                table: "PurchaseOrders",
                column: "IssuedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_RequestForQuotationAwardId_SupplierId",
                table: "PurchaseOrders",
                columns: new[] { "RequestForQuotationAwardId", "SupplierId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SequenceNumber",
                table: "PurchaseOrders",
                column: "SequenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Status",
                table: "PurchaseOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseOrderLines");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");
        }
    }
}
