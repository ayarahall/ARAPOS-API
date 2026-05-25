using System;
using Ayapos.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ayapos.Api.Migrations
{
    [DbContext(typeof(AyaposDbContext))]
    [Migration("20260411170000_AddTenantLicenseFields")]
    public partial class AddTenantLicenseFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LicenseExpiresAt",
                table: "Tenants",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "DATEADD(month, 1, SYSUTCDATETIME())");

            migrationBuilder.AddColumn<string>(
                name: "LicensePlan",
                table: "Tenants",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "MONTHLY");

            migrationBuilder.AddColumn<DateTime>(
                name: "LicenseStartedAt",
                table: "Tenants",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "LicenseStatus",
                table: "Tenants",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "ACTIVE");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LicenseExpiresAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LicensePlan",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LicenseStartedAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LicenseStatus",
                table: "Tenants");
        }
    }
}
