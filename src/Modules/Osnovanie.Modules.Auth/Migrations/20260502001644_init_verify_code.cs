using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Osnovanie.Modules.Auth.Migrations
{
    /// <inheritdoc />
    public partial class init_verify_code : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "phone_verification_codes",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    code_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phone_verification_codes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_phone_verification_codes_expires_at_utc",
                schema: "auth",
                table: "phone_verification_codes",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_phone_verification_codes_phone",
                schema: "auth",
                table: "phone_verification_codes",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "ix_phone_verification_codes_phone_created_at",
                schema: "auth",
                table: "phone_verification_codes",
                columns: new[] { "phone", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "phone_verification_codes",
                schema: "auth");
        }
    }
}
