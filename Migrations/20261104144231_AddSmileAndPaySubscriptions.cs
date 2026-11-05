using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CouplesConnect.Migrations
{
    /// <inheritdoc />
    public partial class AddSmileAndPaySubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SmileAndPayApiKey",
                table: "PlatformSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmileAndPayApiSecret",
                table: "PlatformSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmileAndPayBaseUrl",
                table: "PlatformSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmileAndPayCheckoutHost",
                table: "PlatformSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "zbnet.zb.co.zw");

            migrationBuilder.AddColumn<string>(
                name: "SmileAndPayCurrencyCode",
                table: "PlatformSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "840");

            migrationBuilder.CreateTable(
                name: "SubscriptionPayments",
                columns: table => new
                {
                    SubscriptionPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<int>(type: "int", nullable: false),
                    SubscriptionId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    OrderReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GatewayTransactionReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PaymentUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StatusMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPayments", x => x.SubscriptionPaymentId);
                    table.ForeignKey(
                        name: "FK_SubscriptionPayments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionPayments_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "PackageId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionPayments_UserSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "UserSubscriptions",
                        principalColumn: "SubscriptionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_OrderReference",
                table: "SubscriptionPayments",
                column: "OrderReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_PackageId",
                table: "SubscriptionPayments",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_SubscriptionId",
                table: "SubscriptionPayments",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_UserId",
                table: "SubscriptionPayments",
                column: "UserId",
                unique: true,
                filter: "[Status] = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionPayments");

            migrationBuilder.DropColumn(
                name: "SmileAndPayApiKey",
                table: "PlatformSettings");

            migrationBuilder.DropColumn(
                name: "SmileAndPayApiSecret",
                table: "PlatformSettings");

            migrationBuilder.DropColumn(
                name: "SmileAndPayBaseUrl",
                table: "PlatformSettings");

            migrationBuilder.DropColumn(
                name: "SmileAndPayCheckoutHost",
                table: "PlatformSettings");

            migrationBuilder.DropColumn(
                name: "SmileAndPayCurrencyCode",
                table: "PlatformSettings");
        }
    }
}
