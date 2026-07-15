using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comeback.Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountStatus",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "PendingEmailVerification");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountStatus",
                table: "AspNetUsers");
        }
    }
}
