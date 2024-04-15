using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Document_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesForTheFileDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Gatepass");

            migrationBuilder.DropTable(
                name: "HubConnections");

            migrationBuilder.AlterColumn<string>(
                name: "SubCategory",
                table: "FileDocuments",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "FileDocuments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.CreateIndex(
                name: "IX_Logs_Date",
                table: "Logs",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_FileDocuments_Category",
                table: "FileDocuments",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_FileDocuments_Company",
                table: "FileDocuments",
                column: "Company");

            migrationBuilder.CreateIndex(
                name: "IX_FileDocuments_DateUploaded",
                table: "FileDocuments",
                column: "DateUploaded");

            migrationBuilder.CreateIndex(
                name: "IX_FileDocuments_Name",
                table: "FileDocuments",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileDocuments_OriginalFilename",
                table: "FileDocuments",
                column: "OriginalFilename",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileDocuments_Year",
                table: "FileDocuments",
                column: "Year");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Logs_Date",
                table: "Logs");

            migrationBuilder.DropIndex(
                name: "IX_FileDocuments_Category",
                table: "FileDocuments");

            migrationBuilder.DropIndex(
                name: "IX_FileDocuments_Company",
                table: "FileDocuments");

            migrationBuilder.DropIndex(
                name: "IX_FileDocuments_DateUploaded",
                table: "FileDocuments");

            migrationBuilder.DropIndex(
                name: "IX_FileDocuments_Name",
                table: "FileDocuments");

            migrationBuilder.DropIndex(
                name: "IX_FileDocuments_OriginalFilename",
                table: "FileDocuments");

            migrationBuilder.DropIndex(
                name: "IX_FileDocuments_Year",
                table: "FileDocuments");

            migrationBuilder.AlterColumn<string>(
                name: "SubCategory",
                table: "FileDocuments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "FileDocuments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "Gatepass",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Area = table.Column<string>(type: "text", nullable: false),
                    Contact = table.Column<long>(type: "bigint", nullable: false),
                    DateRequested = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    Items = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MiddleName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Schedule = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gatepass", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HubConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConnectionId = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HubConnections", x => x.Id);
                });
        }
    }
}
