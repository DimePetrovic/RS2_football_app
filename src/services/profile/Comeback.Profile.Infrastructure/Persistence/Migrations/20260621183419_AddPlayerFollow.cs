using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comeback.Profile.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerFollow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_follows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    follower_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    followed_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_follows", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_follows_followed_user_id",
                table: "player_follows",
                column: "followed_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_follows_follower_user_id_followed_user_id",
                table: "player_follows",
                columns: new[] { "follower_user_id", "followed_user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_follows");
        }
    }
}
