using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comeback.Profile.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPositionAndAddCanPlayGoalkeeper : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "player_type",
                table: "profiles",
                newName: "preferred_position");

            migrationBuilder.AddColumn<bool>(
                name: "can_play_goalkeeper",
                table: "profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "can_play_goalkeeper",
                table: "profiles");

            migrationBuilder.RenameColumn(
                name: "preferred_position",
                table: "profiles",
                newName: "player_type");
        }
    }
}
