using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comeback.Match.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchPlayerReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "match_player_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverallRating = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: false),
                    GoalkeepingRating = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: true),
                    DefenseRating = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: true),
                    AttackRating = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: true),
                    EffortRating = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: true),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_player_reviews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_match_player_reviews_MatchId",
                table: "match_player_reviews",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_match_player_reviews_MatchId_ReviewerParticipantId_Reviewed~",
                table: "match_player_reviews",
                columns: new[] { "MatchId", "ReviewerParticipantId", "ReviewedParticipantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "match_player_reviews");
        }
    }
}
