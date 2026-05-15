using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Osnovanie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VLavkeCustomerConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_VLavkeCustomerProfiles",
                table: "VLavkeCustomerProfiles");

            migrationBuilder.RenameTable(
                name: "VLavkeCustomerProfiles",
                newName: "customer_profiles",
                newSchema: "vlavke");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                schema: "vlavke",
                table: "customer_profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "vlavke",
                table: "customer_profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_customer_profiles1",
                schema: "vlavke",
                table: "customer_profiles",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_profiles_CityId1",
                schema: "vlavke",
                table: "customer_profiles",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_profiles_UserId1",
                schema: "vlavke",
                table: "customer_profiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_customer_profiles1",
                schema: "vlavke",
                table: "customer_profiles");

            migrationBuilder.DropIndex(
                name: "IX_customer_profiles_CityId1",
                schema: "vlavke",
                table: "customer_profiles");

            migrationBuilder.DropIndex(
                name: "IX_customer_profiles_UserId1",
                schema: "vlavke",
                table: "customer_profiles");

            migrationBuilder.RenameTable(
                name: "customer_profiles",
                schema: "vlavke",
                newName: "VLavkeCustomerProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "VLavkeCustomerProfiles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "VLavkeCustomerProfiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_VLavkeCustomerProfiles",
                table: "VLavkeCustomerProfiles",
                column: "Id");
        }
    }
}
