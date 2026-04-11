using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddOcrProcessingState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OcrAttemptCount",
                table: "FileDocuments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "OcrCompletedAt",
                table: "FileDocuments",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrError",
                table: "FileDocuments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "OcrQueuedAt",
                table: "FileDocuments",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OcrStartedAt",
                table: "FileDocuments",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrStatus",
                table: "FileDocuments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_FileDocuments_OcrStatus",
                table: "FileDocuments",
                column: "OcrStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FileDocuments_OcrStatus",
                table: "FileDocuments");

            migrationBuilder.DropColumn(
                name: "OcrAttemptCount",
                table: "FileDocuments");

            migrationBuilder.DropColumn(
                name: "OcrCompletedAt",
                table: "FileDocuments");

            migrationBuilder.DropColumn(
                name: "OcrError",
                table: "FileDocuments");

            migrationBuilder.DropColumn(
                name: "OcrQueuedAt",
                table: "FileDocuments");

            migrationBuilder.DropColumn(
                name: "OcrStartedAt",
                table: "FileDocuments");

            migrationBuilder.DropColumn(
                name: "OcrStatus",
                table: "FileDocuments");
        }
    }
}
