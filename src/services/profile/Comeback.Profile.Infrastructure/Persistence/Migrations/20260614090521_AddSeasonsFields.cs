using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comeback.Profile.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "senior_seasons",
                table: "profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "youth_seasons",
                table: "profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "senior_seasons",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "youth_seasons",
                table: "profiles");
        }
    }
}
