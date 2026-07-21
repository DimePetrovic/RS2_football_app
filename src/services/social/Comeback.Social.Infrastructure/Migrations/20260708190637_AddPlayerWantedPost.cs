using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comeback.Social.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerWantedPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_posts_MatchId",
                table: "posts");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "posts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizerDisplayName",
                table: "posts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizerUserId",
                table: "posts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "posts",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartsAt",
                table: "posts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_posts_MatchId_Type",
                table: "posts",
                columns: new[] { "MatchId", "Type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_posts_MatchId_Type",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "OrganizerDisplayName",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "OrganizerUserId",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "StartsAt",
                table: "posts");

            migrationBuilder.CreateIndex(
                name: "IX_posts_MatchId",
                table: "posts",
                column: "MatchId",
                unique: true);
        }
    }
}
