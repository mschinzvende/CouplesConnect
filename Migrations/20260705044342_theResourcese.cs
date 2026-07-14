using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CouplesConnect.Migrations
{
    /// <inheritdoc />
    public partial class theResourcese : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "EstimatedMinutes",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "TimesLaunched",
                table: "Resources");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Difficulty",
                table: "Resources",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EstimatedMinutes",
                table: "Resources",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TimesLaunched",
                table: "Resources",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
