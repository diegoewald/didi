using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalErp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestForQuotations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequestForQuotations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PurchaseRequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SentByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancellationReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestForQuotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestForQuotations_PurchaseRequests_PurchaseRequestId",
                        column: x => x.PurchaseRequestId,
                        principalTable: "PurchaseRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestForQuotations_Users_CancelledByUserId",
                        column: x => x.CancelledByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestForQuotations_Users_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestForQuotations_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestForQuotations_Users_SentByUserId",
                        column: x => x.SentByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestForQuotationLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequestForQuotationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    PurchaseRequestLineId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestForQuotationLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestForQuotationLines_PurchaseRequestLines_PurchaseRequestLineId",
                        column: x => x.PurchaseRequestLineId,
                        principalTable: "PurchaseRequestLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestForQuotationLines_RequestForQuotations_RequestForQuotationId",
                        column: x => x.RequestForQuotationId,
                        principalTable: "RequestForQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestForQuotationSuppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequestForQuotationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupplierId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestForQuotationSuppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestForQuotationSuppliers_RequestForQuotations_RequestForQuotationId",
                        column: x => x.RequestForQuotationId,
                        principalTable: "RequestForQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestForQuotationSuppliers_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierQuotations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequestForQuotationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupplierId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    SupplierReference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ValidUntilDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FinalizedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FinalizedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierQuotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierQuotations_RequestForQuotations_RequestForQuotationId",
                        column: x => x.RequestForQuotationId,
                        principalTable: "RequestForQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierQuotations_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierQuotations_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierQuotations_Users_FinalizedByUserId",
                        column: x => x.FinalizedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierQuotationLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupplierQuotationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequestForQuotationLineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<long>(type: "INTEGER", nullable: false),
                    PromisedDeliveryDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierQuotationLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierQuotationLines_RequestForQuotationLines_RequestForQuotationLineId",
                        column: x => x.RequestForQuotationLineId,
                        principalTable: "RequestForQuotationLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierQuotationLines_SupplierQuotations_SupplierQuotationId",
                        column: x => x.SupplierQuotationId,
                        principalTable: "SupplierQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotationLines_PurchaseRequestLineId",
                table: "RequestForQuotationLines",
                column: "PurchaseRequestLineId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotationLines_RequestForQuotationId_LineNumber",
                table: "RequestForQuotationLines",
                columns: new[] { "RequestForQuotationId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotationLines_RequestForQuotationId_PurchaseRequestLineId",
                table: "RequestForQuotationLines",
                columns: new[] { "RequestForQuotationId", "PurchaseRequestLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotations_CancelledByUserId",
                table: "RequestForQuotations",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotations_ClosedByUserId",
                table: "RequestForQuotations",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotations_CreatedAtUtc",
                table: "RequestForQuotations",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotations_CreatedByUserId",
                table: "RequestForQuotations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotations_PurchaseRequestId",
                table: "RequestForQuotations",
                column: "PurchaseRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotations_SentByUserId",
                table: "RequestForQuotations",
                column: "SentByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotations_Status",
                table: "RequestForQuotations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotationSuppliers_RequestForQuotationId_SupplierId",
                table: "RequestForQuotationSuppliers",
                columns: new[] { "RequestForQuotationId", "SupplierId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotationSuppliers_SupplierId",
                table: "RequestForQuotationSuppliers",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotationLines_RequestForQuotationLineId",
                table: "SupplierQuotationLines",
                column: "RequestForQuotationLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotationLines_SupplierQuotationId_RequestForQuotationLineId",
                table: "SupplierQuotationLines",
                columns: new[] { "SupplierQuotationId", "RequestForQuotationLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotations_CreatedByUserId",
                table: "SupplierQuotations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotations_FinalizedByUserId",
                table: "SupplierQuotations",
                column: "FinalizedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotations_RequestForQuotationId_SupplierId",
                table: "SupplierQuotations",
                columns: new[] { "RequestForQuotationId", "SupplierId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotations_SupplierId",
                table: "SupplierQuotations",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestForQuotationSuppliers");

            migrationBuilder.DropTable(
                name: "SupplierQuotationLines");

            migrationBuilder.DropTable(
                name: "RequestForQuotationLines");

            migrationBuilder.DropTable(
                name: "SupplierQuotations");

            migrationBuilder.DropTable(
                name: "RequestForQuotations");
        }
    }
}
