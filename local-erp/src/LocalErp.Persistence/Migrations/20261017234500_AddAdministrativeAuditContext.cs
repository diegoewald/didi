using System;
using LocalErp.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalErp.Persistence.Migrations
{
    [DbContext(typeof(ErpDbContext))]
    [Migration("20261017234500_AddAdministrativeAuditContext")]
    public partial class AddAdministrativeAuditContext : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActorUserId",
                table: "SecurityAuditEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "SecurityAuditEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_ActorUserId_OccurredAtUtc",
                table: "SecurityAuditEvents",
                columns: new[] { "ActorUserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_RoleId_OccurredAtUtc",
                table: "SecurityAuditEvents",
                columns: new[] { "RoleId", "OccurredAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_SecurityAuditEvents_Roles_RoleId",
                table: "SecurityAuditEvents",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SecurityAuditEvents_Users_ActorUserId",
                table: "SecurityAuditEvents",
                column: "ActorUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SecurityAuditEvents_Roles_RoleId",
                table: "SecurityAuditEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_SecurityAuditEvents_Users_ActorUserId",
                table: "SecurityAuditEvents");

            migrationBuilder.DropIndex(
                name: "IX_SecurityAuditEvents_ActorUserId_OccurredAtUtc",
                table: "SecurityAuditEvents");

            migrationBuilder.DropIndex(
                name: "IX_SecurityAuditEvents_RoleId_OccurredAtUtc",
                table: "SecurityAuditEvents");

            migrationBuilder.DropColumn(
                name: "ActorUserId",
                table: "SecurityAuditEvents");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "SecurityAuditEvents");
        }
    }
}
