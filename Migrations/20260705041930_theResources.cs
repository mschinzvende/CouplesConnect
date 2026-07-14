using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CouplesConnect.Migrations
{
    /// <inheritdoc />
    public partial class theResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resources_ResourceCategory_CategoryId",
                table: "Resources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ResourceCategory",
                table: "ResourceCategory");

            migrationBuilder.RenameTable(
                name: "ResourceCategory",
                newName: "ResourceCategories");

            migrationBuilder.AlterColumn<string>(
                name: "Thumbnail",
                table: "Resources",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string>(
                name: "HtmlPage",
                table: "Resources",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "EstimatedDuration",
                table: "Resources",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Resources",
                type: "nvarchar(600)",
                maxLength: 600,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

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

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Resources",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TimesLaunched",
                table: "Resources",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ResourceCategories",
                table: "ResourceCategories",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_ResourceCategories_CategoryId",
                table: "Resources",
                column: "CategoryId",
                principalTable: "ResourceCategories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resources_ResourceCategories_CategoryId",
                table: "Resources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ResourceCategories",
                table: "ResourceCategories");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "EstimatedMinutes",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "TimesLaunched",
                table: "Resources");

            migrationBuilder.RenameTable(
                name: "ResourceCategories",
                newName: "ResourceCategory");

            migrationBuilder.AlterColumn<string>(
                name: "Thumbnail",
                table: "Resources",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "HtmlPage",
                table: "Resources",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "EstimatedDuration",
                table: "Resources",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Resources",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(600)",
                oldMaxLength: 600);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ResourceCategory",
                table: "ResourceCategory",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_ResourceCategory_CategoryId",
                table: "Resources",
                column: "CategoryId",
                principalTable: "ResourceCategory",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
