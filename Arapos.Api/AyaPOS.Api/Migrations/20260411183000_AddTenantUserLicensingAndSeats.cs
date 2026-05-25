using System;
using Ayapos.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ayapos.Api.Migrations
{
    [DbContext(typeof(AyaposDbContext))]
    [Migration("20260411183000_AddTenantUserLicensingAndSeats")]
    public partial class AddTenantUserLicensingAndSeats : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxUsers",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "LicenseExpiresAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "DATEADD(month, 1, SYSUTCDATETIME())");

            migrationBuilder.AddColumn<string>(
                name: "LicensePlan",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "MONTHLY");

            migrationBuilder.AddColumn<DateTime>(
                name: "LicenseStartedAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "LicenseStatus",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "ACTIVE");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxUsers",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LicenseExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LicensePlan",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LicenseStartedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LicenseStatus",
                table: "Users");
        }
    }
}
