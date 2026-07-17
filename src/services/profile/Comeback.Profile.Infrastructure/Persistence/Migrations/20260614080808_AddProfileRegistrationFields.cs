using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comeback.Profile.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileRegistrationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "position",
                table: "profiles");

            migrationBuilder.AddColumn<DateOnly>(
                name: "date_of_birth",
                table: "profiles",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                table: "profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                table: "profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "player_type",
                table: "profiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "date_of_birth",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "first_name",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "last_name",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "player_type",
                table: "profiles");

            migrationBuilder.AddColumn<string>(
                name: "position",
                table: "profiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
