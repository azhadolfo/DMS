using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Document_Management.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostCleanUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Account",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeNumber = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    LastName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Department = table.Column<string>(type: "text", nullable: false),
                    AccessFolders = table.Column<string>(type: "text", nullable: false),
                    ModuleAccess = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Account", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    OriginalFilename = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    Company = table.Column<string>(type: "text", nullable: false),
                    Year = table.Column<string>(type: "text", nullable: true),
                    Department = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DateUploaded = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: false),
                    SubCategory = table.Column<string>(type: "text", nullable: false),
                    NumberOfPages = table.Column<int>(type: "integer", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    IsInCloudStorage = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Activity = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_Logs_Date",
                table: "Logs",
                column: "Date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Account");

            migrationBuilder.DropTable(
                name: "FileDocuments");

            migrationBuilder.DropTable(
                name: "Logs");
        }
    }
}
