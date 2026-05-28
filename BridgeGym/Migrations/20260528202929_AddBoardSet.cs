using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BridgeGym.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoardSets",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardSets", x => x.Id);
                }
            );

            // Insert a default BoardSet for existing boards
            migrationBuilder.Sql(
                "INSERT INTO \"BoardSets\" (\"Name\", \"CreatedAt\") VALUES ('Initial Set', NOW())"
            );

            migrationBuilder.AddColumn<int>(
                name: "BoardSetId",
                table: "Boards",
                type: "integer",
                nullable: false,
                defaultValue: 1
            ); // Point to the default BoardSet

            migrationBuilder.CreateIndex(
                name: "IX_Boards_BoardSetId",
                table: "Boards",
                column: "BoardSetId"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Boards_BoardSets_BoardSetId",
                table: "Boards",
                column: "BoardSetId",
                principalTable: "BoardSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Boards_BoardSets_BoardSetId",
                table: "Boards"
            );

            migrationBuilder.DropTable(name: "BoardSets");

            migrationBuilder.DropIndex(name: "IX_Boards_BoardSetId", table: "Boards");

            migrationBuilder.DropColumn(name: "BoardSetId", table: "Boards");
        }
    }
}
