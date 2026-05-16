using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BridgeGym.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "MaxTimeSeconds",
                table: "GameSessions",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MinTimeSeconds",
                table: "GameSessions",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "StdDevTimeSeconds",
                table: "GameSessions",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxTimeSeconds",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "MinTimeSeconds",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "StdDevTimeSeconds",
                table: "GameSessions");
        }
    }
}
