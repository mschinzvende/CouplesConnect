using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CouplesConnect.Migrations
{
    /// <inheritdoc />
    public partial class RemovePaynowSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaynowIntegrationId",
                table: "PlatformSettings");

            migrationBuilder.DropColumn(
                name: "PaynowIntegrationKey",
                table: "PlatformSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaynowIntegrationId",
                table: "PlatformSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaynowIntegrationKey",
                table: "PlatformSettings",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
