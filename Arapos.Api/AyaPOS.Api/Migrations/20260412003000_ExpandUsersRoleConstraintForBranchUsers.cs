using Ayapos.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ayapos.Api.Migrations
{
    [DbContext(typeof(AyaposDbContext))]
    [Migration("20260412003000_ExpandUsersRoleConstraintForBranchUsers")]
    public partial class ExpandUsersRoleConstraintForBranchUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Users_Role')
                BEGIN
                    ALTER TABLE [Users] DROP CONSTRAINT [CK_Users_Role];
                END
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE [Users]
                ADD CONSTRAINT [CK_Users_Role]
                CHECK ([Role] IN (N'OWNER', N'TENANT', N'CASHIER', N'HR', N'BRANCH_MANAGER', N'ADMIN', N'admin', N'cashier'));
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Users_Role')
                BEGIN
                    ALTER TABLE [Users] DROP CONSTRAINT [CK_Users_Role];
                END
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE [Users]
                ADD CONSTRAINT [CK_Users_Role]
                CHECK ([Role] IN (N'OWNER', N'TENANT', N'CASHIER', N'ADMIN', N'admin', N'cashier'));
                """);
        }
    }
}
