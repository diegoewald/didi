using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalErp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestForQuotationAwards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequestForQuotationAwards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequestForQuotationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    FinalizedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FinalizedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestForQuotationAwards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestForQuotationAwards_RequestForQuotations_RequestForQuotationId",
                        column: x => x.RequestForQuotationId,
                        principalTable: "RequestForQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestForQuotationAwards_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestForQuotationAwards_Users_FinalizedByUserId",
                        column: x => x.FinalizedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestForQuotationAwardLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequestForQuotationAwardId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequestForQuotationLineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupplierQuotationLineId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestForQuotationAwardLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestForQuotationAwardLines_RequestForQuotationAwards_RequestForQuotationAwardId",
                        column: x => x.RequestForQuotationAwardId,
                        principalTable: "RequestForQuotationAwards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestForQuotationAwardLines_RequestForQuotationLines_RequestForQuotationLineId",
                        column: x => x.RequestForQuotationLineId,
                        principalTable: "RequestForQuotationLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestForQuotationAwardLines_SupplierQuotationLines_SupplierQuotationLineId",
                        column: x => x.SupplierQuotationLineId,
                        principalTable: "SupplierQuotationLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotationAwardLines_RequestForQuotationAwardId_RequestForQuotationLineId",
                table: "RequestForQuotationAwardLines",
                columns: new[] { "RequestForQuotationAwardId", "RequestForQuotationLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotationAwardLines_RequestForQuotationLineId",
                table: "RequestForQuotationAwardLines",
                column: "RequestForQuotationLineId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotationAwardLines_SupplierQuotationLineId",
                table: "RequestForQuotationAwardLines",
                column: "SupplierQuotationLineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotationAwards_CreatedAtUtc",
                table: "RequestForQuotationAwards",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotationAwards_CreatedByUserId",
                table: "RequestForQuotationAwards",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotationAwards_FinalizedByUserId",
                table: "RequestForQuotationAwards",
                column: "FinalizedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotationAwards_RequestForQuotationId",
                table: "RequestForQuotationAwards",
                column: "RequestForQuotationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotationAwards_Status",
                table: "RequestForQuotationAwards",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestForQuotationAwardLines");

            migrationBuilder.DropTable(
                name: "RequestForQuotationAwards");
        }
    }
}
