using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleAttributesinRegister : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Account",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Account");
        }
    }
}