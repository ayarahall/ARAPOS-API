using Ayapos.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ayapos.Api.Migrations
{
    [DbContext(typeof(AyaposDbContext))]
    [Migration("20260504090000_AddBranchReceiptBranding")]
    public partial class AddBranchReceiptBranding : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BranchSettings') AND name = 'CompanyAddress')
                    ALTER TABLE [BranchSettings] ADD [CompanyAddress] nvarchar(220) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BranchSettings') AND name = 'CompanyLogoUrl')
                    ALTER TABLE [BranchSettings] ADD [CompanyLogoUrl] nvarchar(500) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BranchSettings') AND name = 'CompanyName')
                    ALTER TABLE [BranchSettings] ADD [CompanyName] nvarchar(160) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BranchSettings') AND name = 'CompanyPhone')
                    ALTER TABLE [BranchSettings] ADD [CompanyPhone] nvarchar(40) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BranchSettings') AND name = 'CompanyTaxNumber')
                    ALTER TABLE [BranchSettings] ADD [CompanyTaxNumber] nvarchar(80) NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE bs
                SET bs.CompanyName = COALESCE(NULLIF(bs.CompanyName, N''), b.[Name])
                FROM [BranchSettings] bs
                INNER JOIN [Branches] b
                    ON b.[TenantId] = bs.[TenantId]
                   AND b.[Id] = bs.[BranchId];
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BranchSettings') AND name = 'CompanyAddress')
                    ALTER TABLE [BranchSettings] DROP COLUMN [CompanyAddress];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BranchSettings') AND name = 'CompanyLogoUrl')
                    ALTER TABLE [BranchSettings] DROP COLUMN [CompanyLogoUrl];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BranchSettings') AND name = 'CompanyName')
                    ALTER TABLE [BranchSettings] DROP COLUMN [CompanyName];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BranchSettings') AND name = 'CompanyPhone')
                    ALTER TABLE [BranchSettings] DROP COLUMN [CompanyPhone];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BranchSettings') AND name = 'CompanyTaxNumber')
                    ALTER TABLE [BranchSettings] DROP COLUMN [CompanyTaxNumber];
                """);
        }
    }
}
