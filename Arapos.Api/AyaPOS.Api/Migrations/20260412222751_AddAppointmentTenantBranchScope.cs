using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ayapos.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentTenantBranchScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'BranchId')
                    ALTER TABLE [Appointments] ADD [BranchId] uniqueidentifier NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'TenantId')
                    ALTER TABLE [Appointments] ADD [TenantId] uniqueidentifier NULL;
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_TenantId' AND object_id = OBJECT_ID('Customers'))
                    CREATE INDEX [IX_Customers_TenantId] ON [Customers] ([TenantId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Appointments_Tenant_Branch_StartAt' AND object_id = OBJECT_ID('Appointments'))
                    CREATE INDEX [IX_Appointments_Tenant_Branch_StartAt] ON [Appointments] ([TenantId], [BranchId], [StartAt]);
                """);

            migrationBuilder.Sql(
                """
                UPDATE appointment
                SET appointment.TenantId = customer.TenantId
                FROM Appointments AS appointment
                INNER JOIN Customers AS customer ON customer.Id = appointment.CustomerId
                WHERE appointment.TenantId IS NULL
                  AND customer.TenantId IS NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE appointment
                SET appointment.BranchId = assignment.BranchId
                FROM Appointments AS appointment
                INNER JOIN BranchUserAssignments AS assignment
                    ON assignment.UserId = appointment.CreatedByUserId
                   AND assignment.TenantId = appointment.TenantId
                WHERE appointment.BranchId IS NULL
                  AND appointment.TenantId IS NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                ;WITH SingleBranchPerTenant AS
                (
                    SELECT TenantId, MIN(Id) AS BranchId
                    FROM Branches
                    GROUP BY TenantId
                    HAVING COUNT(*) = 1
                )
                UPDATE appointment
                SET appointment.BranchId = branchChoice.BranchId
                FROM Appointments AS appointment
                INNER JOIN SingleBranchPerTenant AS branchChoice
                    ON branchChoice.TenantId = appointment.TenantId
                WHERE appointment.BranchId IS NULL
                  AND appointment.TenantId IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Appointments_Tenant_Branch_StartAt' AND object_id = OBJECT_ID('Appointments'))
                    DROP INDEX [IX_Appointments_Tenant_Branch_StartAt] ON [Appointments];
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'BranchId')
                    ALTER TABLE [Appointments] DROP COLUMN [BranchId];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'TenantId')
                    ALTER TABLE [Appointments] DROP COLUMN [TenantId];
                """);
        }
    }
}
