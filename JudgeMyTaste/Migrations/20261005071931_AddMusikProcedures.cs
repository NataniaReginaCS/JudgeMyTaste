using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JudgeMyTaste.Migrations
{
    /// <inheritdoc />
    public partial class AddMusikProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            CREATE PROCEDURE dbo.usp_Musik_GetAll
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT Id, Name, Genre FROM dbo.Musik ORDER BY Name;
            END");

            migrationBuilder.Sql(@"
            CREATE PROCEDURE dbo.usp_Musik_Insert
                @Name NVARCHAR(100),
                @Genre NVARCHAR(50) = NULL
            AS
            BEGIN
                SET NOCOUNT ON;
                IF @Name IS NULL OR LTRIM(RTRIM(@Name)) = N''
                    THROW 50001, 'Judul lagu (Name) wajib diisi.', 1;
                INSERT INTO dbo.Musik (Name, Genre)
                VALUES (LTRIM(RTRIM(@Name)), NULLIF(LTRIM(RTRIM(@Genre)), N''));
            END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE dbo.usp_Musik_Insert");
            migrationBuilder.Sql("DROP PROCEDURE dbo.usp_Musik_GetAll");
        }
    }
}
