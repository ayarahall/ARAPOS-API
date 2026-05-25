using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ayapos.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCashierSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashierSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpeningCashCents = table.Column<int>(type: "int", nullable: false),
                    TotalCashCents = table.Column<int>(type: "int", nullable: false),
                    TotalCardCents = table.Column<int>(type: "int", nullable: false),
                    TotalTransferCents = table.Column<int>(type: "int", nullable: false),
                    TotalRefundCents = table.Column<int>(type: "int", nullable: false),
                    ExpectedCashCents = table.Column<int>(type: "int", nullable: false),
                    ActualCashCents = table.Column<int>(type: "int", nullable: false),
                    DifferenceCents = table.Column<int>(type: "int", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashierSessions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashierSessions");
        }
    }
}
