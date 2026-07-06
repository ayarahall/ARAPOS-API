using System;
using Ayapos.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ayapos.Api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AyaposDbContext))]
    [Migration("20260706120000_AddDocumentSoftDelete")]
    public partial class AddDocumentSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "DocumentUploads",
                type: "timestamp(0) with time zone",
                precision: 0,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "DocumentUploads",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentUploads_Tenant_Branch_DeletedAt",
                table: "DocumentUploads",
                columns: new[] { "TenantId", "BranchId", "DeletedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocumentUploads_Tenant_Branch_DeletedAt",
                table: "DocumentUploads");

            migrationBuilder.DropColumn(name: "DeletedByUserId", table: "DocumentUploads");
            migrationBuilder.DropColumn(name: "DeletedAt", table: "DocumentUploads");
        }
    }
}
