using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrumPokerAPI.Migrations
{
    /// <inheritdoc />
    public partial class ParticipantIsPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_player",
                table: "Participants",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_player",
                table: "Participants");
        }
    }
}
