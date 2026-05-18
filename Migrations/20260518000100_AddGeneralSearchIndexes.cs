using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document_Management.Migrations
{
    [DbContext(typeof(Data.ApplicationDbContext))]
    [Migration("20260518000100_AddGeneralSearchIndexes")]
    public partial class AddGeneralSearchIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE EXTENSION IF NOT EXISTS pg_trgm;
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_FileDocuments_SearchScope"
                ON "FileDocuments" ("Company", "Department", "DateUploaded" DESC)
                WHERE NOT "IsDeleted";
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_FileDocuments_Description_Trgm"
                ON "FileDocuments" USING gin ("Description" gin_trgm_ops)
                WHERE NOT "IsDeleted";
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_FileDocuments_OriginalFilename_Trgm"
                ON "FileDocuments" USING gin ("OriginalFilename" gin_trgm_ops)
                WHERE NOT "IsDeleted";
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_FileDocuments_BoxNumber_Trgm"
                ON "FileDocuments" USING gin ("BoxNumber" gin_trgm_ops)
                WHERE NOT "IsDeleted";
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_FileDocuments_BoxNumber_Trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_FileDocuments_OriginalFilename_Trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_FileDocuments_Description_Trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_FileDocuments_SearchScope";""");
        }
    }
}
