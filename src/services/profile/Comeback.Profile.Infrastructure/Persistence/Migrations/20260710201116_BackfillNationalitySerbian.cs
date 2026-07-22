using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comeback.Profile.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Data migration: profiles created before the nationality feature default to Serbian (RS).
    /// Only fills empty values — never overwrites a nationality the player has already chosen.
    /// </summary>
    public partial class BackfillNationalitySerbian : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """UPDATE profiles SET "Nationality" = 'RS' WHERE "Nationality" IS NULL;""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible data fix: we cannot tell backfilled rows from user-chosen ones.
        }
    }
}
