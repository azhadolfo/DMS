using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddMigrationForUniqueFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubCategories_SubCategoryName",
                table: "SubCategories");

            migrationBuilder.DropIndex(
                name: "IX_Departments_DepartmentName",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Companies_CompanyName",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Categories_CategoryName",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_SubCategories_SubCategoryName",
                table: "SubCategories",
                column: "SubCategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DepartmentName",
                table: "Departments",
                column: "DepartmentName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CompanyName",
                table: "Companies",
                column: "CompanyName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CategoryName",
                table: "Categories",
                column: "CategoryName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubCategories_SubCategoryName",
                table: "SubCategories");

            migrationBuilder.DropIndex(
                name: "IX_Departments_DepartmentName",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Companies_CompanyName",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Categories_CategoryName",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_SubCategories_SubCategoryName",
                table: "SubCategories",
                column: "SubCategoryName");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DepartmentName",
                table: "Departments",
                column: "DepartmentName");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CompanyName",
                table: "Companies",
                column: "CompanyName");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CategoryName",
                table: "Categories",
                column: "CategoryName");
        }
    }
}
