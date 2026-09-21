using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostAndFound.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260920120000_AddItemPosts")]
public partial class AddItemPosts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ItemPosts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Type = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Category = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                ItemDate = table.Column<DateOnly>(type: "date", nullable: false),
                ContactPreference = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ItemPosts", x => x.Id);
                table.ForeignKey(
                    name: "FK_ItemPosts_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ItemPostImages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ItemPostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Path = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ItemPostImages", x => x.Id);
                table.ForeignKey(
                    name: "FK_ItemPostImages_ItemPosts_ItemPostId",
                    column: x => x.ItemPostId,
                    principalTable: "ItemPosts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_ItemPosts_UserId", table: "ItemPosts", column: "UserId");
        migrationBuilder.CreateIndex(name: "IX_ItemPostImages_ItemPostId", table: "ItemPostImages", column: "ItemPostId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ItemPostImages");
        migrationBuilder.DropTable(name: "ItemPosts");
    }
}
