using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comeback.Match.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGuest",
                table: "match_participants",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsGuest",
                table: "match_participants");
        }
    }
}
