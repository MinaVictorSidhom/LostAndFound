using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostAndFound.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261018140000_AddItemPostMessages")]
public partial class AddItemPostMessages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The removed ShowEmail enum value was stored as 1; move those reports to messaging only.
        migrationBuilder.Sql("UPDATE ItemPosts SET ContactPreference = 0 WHERE ContactPreference = 1");

        migrationBuilder.CreateTable(
            name: "ItemPostMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ItemPostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SenderUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RecipientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ItemPostMessages", x => x.Id);
                table.ForeignKey("FK_ItemPostMessages_AspNetUsers_RecipientUserId", x => x.RecipientUserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ItemPostMessages_AspNetUsers_SenderUserId", x => x.SenderUserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ItemPostMessages_ItemPosts_ItemPostId", x => x.ItemPostId, "ItemPosts", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_ItemPostMessages_ConversationId", "ItemPostMessages", "ConversationId");
        migrationBuilder.CreateIndex("IX_ItemPostMessages_ItemPostId", "ItemPostMessages", "ItemPostId");
        migrationBuilder.CreateIndex("IX_ItemPostMessages_RecipientUserId", "ItemPostMessages", "RecipientUserId");
        migrationBuilder.CreateIndex("IX_ItemPostMessages_SenderUserId", "ItemPostMessages", "SenderUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "ItemPostMessages");
}
