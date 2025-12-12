using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddedBoxNumberAndSubmittedByAndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BoxNumber",
                table: "FileDocuments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubmittedBy",
                table: "FileDocuments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "SubmittedDate",
                table: "FileDocuments",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BoxNumber",
                table: "FileDocuments");

            migrationBuilder.DropColumn(
                name: "SubmittedBy",
                table: "FileDocuments");

            migrationBuilder.DropColumn(
                name: "SubmittedDate",
                table: "FileDocuments");
        }
    }
}
