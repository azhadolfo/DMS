using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddcolumnAreaandItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Area",
                table: "Gatepass",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Items",
                table: "Gatepass",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Area",
                table: "Gatepass");

            migrationBuilder.DropColumn(
                name: "Items",
                table: "Gatepass");
        }
    }
}