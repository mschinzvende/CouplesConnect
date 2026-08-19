using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CouplesConnect.Migrations
{
    /// <inheritdoc />
    public partial class AddForumCommunity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ForumCategories",
                columns: table => new
                {
                    ForumCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumCategories", x => x.ForumCategoryId);
                });

            migrationBuilder.InsertData(
                table: "ForumCategories",
                columns: new[] { "ForumCategoryId", "Color", "CreatedAt", "Description", "DisplayOrder", "Icon", "IsActive", "Name", "Slug" },
                values: new object[,]
                {
                    { new Guid("4c6e9b6a-1e8a-4ee0-b72c-9d9f3fd30c11"), "#4f46e5", new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc), "A welcoming space for everyday questions, stories, and community conversation.", 1, "mdi mdi-forum-outline", true, "General Discussion", "general-discussion" },
                    { new Guid("a0cf614d-78cf-4723-858b-0b30c5feef92"), "#e15b8a", new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc), "Explore affection, intimacy, appreciation, and the small habits that strengthen connection.", 2, "mdi mdi-heart-outline", true, "Love & Connection", "love-connection" },
                    { new Guid("7fa6b8c8-6a54-4257-9b40-8c693a2c1a8d"), "#0f9b8e", new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc), "Share practical ways to listen well, repair tension, and navigate difficult conversations.", 3, "mdi mdi-message-text-outline", true, "Communication & Conflict", "communication-conflict" },
                    { new Guid("e2d2a839-9867-44cc-8f35-13b646b0d8bd"), "#d28b17", new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc), "Talk through family, friendships, wellbeing, shared goals, and the life you are building.", 4, "mdi mdi-account-group-outline", true, "Life, Family & Growth", "life-family-growth" }
                });

            migrationBuilder.CreateTable(
                name: "ForumTopics",
                columns: table => new
                {
                    ForumTopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ForumCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    IsAnonymous = table.Column<bool>(type: "bit", nullable: false),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplyCount = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumTopics", x => x.ForumTopicId);
                    table.ForeignKey(
                        name: "FK_ForumTopics_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ForumTopics_ForumCategories_ForumCategoryId",
                        column: x => x.ForumCategoryId,
                        principalTable: "ForumCategories",
                        principalColumn: "ForumCategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ForumReplies",
                columns: table => new
                {
                    ForumReplyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ForumTopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    IsAnonymous = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumReplies", x => x.ForumReplyId);
                    table.ForeignKey(
                        name: "FK_ForumReplies_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ForumReplies_ForumTopics_ForumTopicId",
                        column: x => x.ForumTopicId,
                        principalTable: "ForumTopics",
                        principalColumn: "ForumTopicId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForumCategories_IsActive_DisplayOrder",
                table: "ForumCategories",
                columns: new[] { "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ForumCategories_Name",
                table: "ForumCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ForumCategories_Slug",
                table: "ForumCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ForumReplies_AuthorId_CreatedAt",
                table: "ForumReplies",
                columns: new[] { "AuthorId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ForumReplies_ForumTopicId_IsDeleted_CreatedAt",
                table: "ForumReplies",
                columns: new[] { "ForumTopicId", "IsDeleted", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ForumTopics_AuthorId_CreatedAt",
                table: "ForumTopics",
                columns: new[] { "AuthorId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ForumTopics_ForumCategoryId_IsDeleted_IsArchived_IsPinned_LastActivityAt",
                table: "ForumTopics",
                columns: new[] { "ForumCategoryId", "IsDeleted", "IsArchived", "IsPinned", "LastActivityAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForumReplies");

            migrationBuilder.DropTable(
                name: "ForumTopics");

            migrationBuilder.DropTable(
                name: "ForumCategories");
        }
    }
}
