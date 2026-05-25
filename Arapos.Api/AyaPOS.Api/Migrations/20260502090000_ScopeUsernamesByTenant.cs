using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ayapos.Api.Migrations
{
    /// <inheritdoc />
    public partial class ScopeUsernamesByTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Users_Username' AND object_id = OBJECT_ID('dbo.Users'))
                    DROP INDEX UX_Users_Username ON dbo.Users;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Username' AND object_id = OBJECT_ID('dbo.Users'))
                    CREATE INDEX IX_Users_Username ON dbo.Users(Username);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Username' AND object_id = OBJECT_ID('dbo.Users'))
                    DROP INDEX IX_Users_Username ON dbo.Users;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Users_Username' AND object_id = OBJECT_ID('dbo.Users'))
                    CREATE UNIQUE INDEX UX_Users_Username ON dbo.Users(Username);
                """);
        }
    }
}
