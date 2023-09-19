using Microsoft.EntityFrameworkCore.Migrations;
using System.Numerics;

#nullable disable

namespace Document_Management.Migrations
{
    /// <inheritdoc />
    public partial class Altercolumncontact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "Contact",
                table: "Gatepass",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(BigInteger),
                oldType: "numeric");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<BigInteger>(
                name: "Contact",
                table: "Gatepass",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}