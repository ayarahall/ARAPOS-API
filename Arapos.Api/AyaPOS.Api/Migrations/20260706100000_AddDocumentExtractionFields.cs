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
    [Migration("20260706100000_AddDocumentExtractionFields")]
    public partial class AddDocumentExtractionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExtractedText",
                table: "DocumentUploads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedFieldsJson",
                table: "DocumentUploads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedFieldsJson",
                table: "DocumentUploads",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ReviewedFieldsJson", table: "DocumentUploads");
            migrationBuilder.DropColumn(name: "ExtractedFieldsJson", table: "DocumentUploads");
            migrationBuilder.DropColumn(name: "ExtractedText", table: "DocumentUploads");
        }
    }
}
