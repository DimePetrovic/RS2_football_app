using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comeback.Match.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "match_goals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScorerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScorerDisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ScoringTeam = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsOwnGoal = table.Column<bool>(type: "boolean", nullable: false),
                    AssistUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssistDisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_goals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_match_goals_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_match_goals_MatchId",
                table: "match_goals",
                column: "MatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "match_goals");
        }
    }
}
