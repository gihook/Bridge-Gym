using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BridgeGym.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAutoCalculated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAutoCalculated",
                table: "BoardHands",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsAutoCalculated", table: "BoardHands");
        }
    }
}
