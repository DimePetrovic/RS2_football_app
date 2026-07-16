using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comeback.Match.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupMatchSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GroupName",
                table: "matches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpponentGroupCaptainDisplayName",
                table: "matches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OpponentGroupCaptainUserId",
                table: "matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OpponentGroupId",
                table: "matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpponentGroupInviteStatus",
                table: "matches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpponentGroupName",
                table: "matches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SecondOrganizerUserId",
                table: "matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GroupSide",
                table: "match_participants",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.CreateIndex(
                name: "IX_matches_GroupId",
                table: "matches",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_matches_OpponentGroupId",
                table: "matches",
                column: "OpponentGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_matches_GroupId",
                table: "matches");

            migrationBuilder.DropIndex(
                name: "IX_matches_OpponentGroupId",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "GroupName",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "OpponentGroupCaptainDisplayName",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "OpponentGroupCaptainUserId",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "OpponentGroupId",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "OpponentGroupInviteStatus",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "OpponentGroupName",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "SecondOrganizerUserId",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "GroupSide",
                table: "match_participants");
        }
    }
}
