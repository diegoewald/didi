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
            migrationBuilder.Sql(
                """
                CREATE TABLE "__new_SecurityAuditEvents" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_SecurityAuditEvents" PRIMARY KEY,
                    "OccurredAtUtc" TEXT NOT NULL,
                    "EventCode" TEXT NOT NULL,
                    "DetailCode" TEXT NULL,
                    "UserId" TEXT NULL,
                    "ActorUserId" TEXT NULL,
                    "RoleId" TEXT NULL,
                    CONSTRAINT "FK_SecurityAuditEvents_Users_UserId"
                        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE SET NULL,
                    CONSTRAINT "FK_SecurityAuditEvents_Users_ActorUserId"
                        FOREIGN KEY ("ActorUserId") REFERENCES "Users" ("Id") ON DELETE SET NULL,
                    CONSTRAINT "FK_SecurityAuditEvents_Roles_RoleId"
                        FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE SET NULL
                );
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "__new_SecurityAuditEvents"
                    ("Id", "OccurredAtUtc", "EventCode", "DetailCode", "UserId", "ActorUserId", "RoleId")
                SELECT
                    "Id", "OccurredAtUtc", "EventCode", "DetailCode", "UserId", NULL, NULL
                FROM "SecurityAuditEvents";
                """);

            migrationBuilder.DropTable(name: "SecurityAuditEvents");

            migrationBuilder.RenameTable(
                name: "__new_SecurityAuditEvents",
                newName: "SecurityAuditEvents");

            CreateIndexes(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE "__old_SecurityAuditEvents" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_SecurityAuditEvents" PRIMARY KEY,
                    "OccurredAtUtc" TEXT NOT NULL,
                    "EventCode" TEXT NOT NULL,
                    "DetailCode" TEXT NULL,
                    "UserId" TEXT NULL,
                    CONSTRAINT "FK_SecurityAuditEvents_Users_UserId"
                        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
                );
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "__old_SecurityAuditEvents"
                    ("Id", "OccurredAtUtc", "EventCode", "DetailCode", "UserId")
                SELECT
                    "Id", "OccurredAtUtc", "EventCode", "DetailCode", "UserId"
                FROM "SecurityAuditEvents";
                """);

            migrationBuilder.DropTable(name: "SecurityAuditEvents");

            migrationBuilder.RenameTable(
                name: "__old_SecurityAuditEvents",
                newName: "SecurityAuditEvents");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_EventCode_OccurredAtUtc",
                table: "SecurityAuditEvents",
                columns: new[] { "EventCode", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_OccurredAtUtc",
                table: "SecurityAuditEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_UserId_OccurredAtUtc",
                table: "SecurityAuditEvents",
                columns: new[] { "UserId", "OccurredAtUtc" });
        }

        private static void CreateIndexes(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_ActorUserId_OccurredAtUtc",
                table: "SecurityAuditEvents",
                columns: new[] { "ActorUserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_EventCode_OccurredAtUtc",
                table: "SecurityAuditEvents",
                columns: new[] { "EventCode", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_OccurredAtUtc",
                table: "SecurityAuditEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_RoleId_OccurredAtUtc",
                table: "SecurityAuditEvents",
                columns: new[] { "RoleId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_UserId_OccurredAtUtc",
                table: "SecurityAuditEvents",
                columns: new[] { "UserId", "OccurredAtUtc" });
        }
    }
}
