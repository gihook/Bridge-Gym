using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BridgeGym.Migrations
{
    /// <inheritdoc />
    public partial class AddCorrectAnswersCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CorrectAnswersCount",
                table: "GameSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrectAnswersCount",
                table: "GameSessions");
        }
    }
}
