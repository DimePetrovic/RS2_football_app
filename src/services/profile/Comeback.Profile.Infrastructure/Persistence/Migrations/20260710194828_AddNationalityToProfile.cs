using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comeback.Profile.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNationalityToProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "profiles",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "profiles");
        }
    }
}
