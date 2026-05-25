using Ayapos.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ayapos.Api.Migrations
{
    [DbContext(typeof(AyaposDbContext))]
    [Migration("20260412113000_AddBranchReceiptPrintSettings")]
    public partial class AddBranchReceiptPrintSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoPrintReceiptAfterPayment",
                table: "BranchSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptFooterNote",
                table: "BranchSettings",
                type: "nvarchar(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptHeaderLine1",
                table: "BranchSettings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptHeaderLine2",
                table: "BranchSettings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptTitle",
                table: "BranchSettings",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Sales Receipt");

            migrationBuilder.AddColumn<bool>(
                name: "ShowBranchNameOnReceipt",
                table: "BranchSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowCustomerNameOnReceipt",
                table: "BranchSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowPaymentHistoryOnReceipt",
                table: "BranchSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql(
                """
                UPDATE bs
                SET
                    bs.ReceiptHeaderLine1 = COALESCE(NULLIF(bs.ReceiptHeaderLine1, N''), b.[Name]),
                    bs.ReceiptFooterNote = COALESCE(NULLIF(bs.ReceiptFooterNote, N''), N'Thank you for visiting AYAPOS.')
                FROM [BranchSettings] bs
                INNER JOIN [Branches] b
                    ON b.[TenantId] = bs.[TenantId]
                   AND b.[Id] = bs.[BranchId];
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AutoPrintReceiptAfterPayment", table: "BranchSettings");
            migrationBuilder.DropColumn(name: "ReceiptFooterNote", table: "BranchSettings");
            migrationBuilder.DropColumn(name: "ReceiptHeaderLine1", table: "BranchSettings");
            migrationBuilder.DropColumn(name: "ReceiptHeaderLine2", table: "BranchSettings");
            migrationBuilder.DropColumn(name: "ReceiptTitle", table: "BranchSettings");
            migrationBuilder.DropColumn(name: "ShowBranchNameOnReceipt", table: "BranchSettings");
            migrationBuilder.DropColumn(name: "ShowCustomerNameOnReceipt", table: "BranchSettings");
            migrationBuilder.DropColumn(name: "ShowPaymentHistoryOnReceipt", table: "BranchSettings");
        }
    }
}
