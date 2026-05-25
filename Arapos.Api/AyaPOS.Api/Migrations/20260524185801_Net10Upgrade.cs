using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ayapos.Api.Migrations
{
    /// <inheritdoc />
    public partial class Net10Upgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'PermissionsJson')
                    ALTER TABLE [Users] ADD [PermissionsJson] nvarchar(max) NULL;
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'BaseSalary')
                    ALTER TABLE [Staff] ADD [BaseSalary] decimal(12,2) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'BranchId')
                    ALTER TABLE [Staff] ADD [BranchId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'DeductionPerAbsentDay')
                    ALTER TABLE [Staff] ADD [DeductionPerAbsentDay] decimal(12,2) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'DeductionPerLateMinute')
                    ALTER TABLE [Staff] ADD [DeductionPerLateMinute] decimal(12,2) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'Email')
                    ALTER TABLE [Staff] ADD [Email] nvarchar(120) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'EmployeeCode')
                    ALTER TABLE [Staff] ADD [EmployeeCode] nvarchar(40) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'EmploymentType')
                    ALTER TABLE [Staff] ADD [EmploymentType] nvarchar(30) NULL DEFAULT 'employee';
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'HireDate')
                    ALTER TABLE [Staff] ADD [HireDate] datetime2(0) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'IsBookableForAppointments')
                    ALTER TABLE [Staff] ADD [IsBookableForAppointments] bit NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'LinkedUserId')
                    ALTER TABLE [Staff] ADD [LinkedUserId] uniqueidentifier NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'Notes')
                    ALTER TABLE [Staff] ADD [Notes] nvarchar(500) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'PhotoUrl')
                    ALTER TABLE [Staff] ADD [PhotoUrl] nvarchar(260) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'SalaryType')
                    ALTER TABLE [Staff] ADD [SalaryType] nvarchar(20) NULL DEFAULT 'monthly';
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'TenantId')
                    ALTER TABLE [Staff] ADD [TenantId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'TrackAttendance')
                    ALTER TABLE [Staff] ADD [TrackAttendance] bit NOT NULL DEFAULT 1;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'WeeklyOffDays')
                    ALTER TABLE [Staff] ADD [WeeklyOffDays] nvarchar(40) NULL;
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'StaffDocuments')
                CREATE TABLE [StaffDocuments] (
                    [Id] uniqueidentifier NOT NULL DEFAULT (newid()),
                    [TenantId] uniqueidentifier NOT NULL,
                    [BranchId] uniqueidentifier NOT NULL,
                    [StaffId] uniqueidentifier NOT NULL,
                    [Title] nvarchar(120) NOT NULL,
                    [DocumentType] nvarchar(40) NOT NULL,
                    [FileName] nvarchar(160) NULL,
                    [FileUrl] nvarchar(260) NULL,
                    [ExpiresAt] datetime2(0) NULL,
                    [CreatedAt] datetime2(0) NOT NULL DEFAULT (sysdatetime()),
                    CONSTRAINT [PK_StaffDocuments] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_StaffDocuments_Branches_Tenant] FOREIGN KEY ([TenantId], [BranchId]) REFERENCES [Branches] ([TenantId], [Id]),
                    CONSTRAINT [FK_StaffDocuments_Staff] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id])
                );
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'StaffLeaves')
                CREATE TABLE [StaffLeaves] (
                    [Id] uniqueidentifier NOT NULL DEFAULT (newid()),
                    [TenantId] uniqueidentifier NOT NULL,
                    [BranchId] uniqueidentifier NOT NULL,
                    [StaffId] uniqueidentifier NOT NULL,
                    [LeaveType] nvarchar(30) NOT NULL,
                    [StartDate] datetime2(0) NOT NULL,
                    [EndDate] datetime2(0) NOT NULL,
                    [IsPaid] bit NOT NULL,
                    [Status] nvarchar(20) NOT NULL DEFAULT 'approved',
                    [Notes] nvarchar(400) NULL,
                    [CreatedAt] datetime2(0) NOT NULL DEFAULT (sysdatetime()),
                    CONSTRAINT [PK_StaffLeaves] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_StaffLeaves_Branches_Tenant] FOREIGN KEY ([TenantId], [BranchId]) REFERENCES [Branches] ([TenantId], [Id]),
                    CONSTRAINT [FK_StaffLeaves_Staff] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id])
                );
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'StaffShifts')
                CREATE TABLE [StaffShifts] (
                    [Id] uniqueidentifier NOT NULL DEFAULT (newid()),
                    [TenantId] uniqueidentifier NOT NULL,
                    [BranchId] uniqueidentifier NOT NULL,
                    [StaffId] uniqueidentifier NOT NULL,
                    [Name] nvarchar(80) NOT NULL,
                    [StartTime] time NOT NULL,
                    [EndTime] time NOT NULL,
                    [GraceMinutes] int NOT NULL,
                    [IsActive] bit NOT NULL DEFAULT 1,
                    [EffectiveFrom] datetime2(0) NULL,
                    [EffectiveTo] datetime2(0) NULL,
                    [WeeklyPattern] nvarchar(40) NULL,
                    [CreatedAt] datetime2(0) NOT NULL DEFAULT (sysdatetime()),
                    CONSTRAINT [PK_StaffShifts] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_StaffShifts_Branches_Tenant] FOREIGN KEY ([TenantId], [BranchId]) REFERENCES [Branches] ([TenantId], [Id]),
                    CONSTRAINT [FK_StaffShifts_Staff] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id])
                );
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'StaffAttendances')
                CREATE TABLE [StaffAttendances] (
                    [Id] uniqueidentifier NOT NULL DEFAULT (newid()),
                    [TenantId] uniqueidentifier NOT NULL,
                    [BranchId] uniqueidentifier NOT NULL,
                    [StaffId] uniqueidentifier NOT NULL,
                    [ShiftId] uniqueidentifier NULL,
                    [AttendanceDate] datetime2(0) NOT NULL,
                    [CheckInAt] datetime2(0) NULL,
                    [CheckOutAt] datetime2(0) NULL,
                    [Status] nvarchar(20) NOT NULL DEFAULT 'present',
                    [LateMinutes] int NOT NULL,
                    [WorkedMinutes] int NOT NULL,
                    [DeductionAmount] decimal(12,2) NOT NULL,
                    [Notes] nvarchar(400) NULL,
                    [CreatedAt] datetime2(0) NOT NULL DEFAULT (sysdatetime()),
                    CONSTRAINT [PK_StaffAttendances] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_StaffAttendances_Branches_Tenant] FOREIGN KEY ([TenantId], [BranchId]) REFERENCES [Branches] ([TenantId], [Id]),
                    CONSTRAINT [FK_StaffAttendances_Staff] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id]),
                    CONSTRAINT [FK_StaffAttendances_StaffShifts] FOREIGN KEY ([ShiftId]) REFERENCES [StaffShifts] ([Id])
                );
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Staff_LinkedUserId' AND object_id = OBJECT_ID('Staff'))
                    CREATE INDEX [IX_Staff_LinkedUserId] ON [Staff] ([LinkedUserId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Staff_Tenant_Branch_Active' AND object_id = OBJECT_ID('Staff'))
                    CREATE INDEX [IX_Staff_Tenant_Branch_Active] ON [Staff] ([TenantId], [BranchId], [IsActive]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Staff_Tenant_Branch_Bookable' AND object_id = OBJECT_ID('Staff'))
                    CREATE INDEX [IX_Staff_Tenant_Branch_Bookable] ON [Staff] ([TenantId], [BranchId], [IsBookableForAppointments]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Staff_Tenant_Branch_EmployeeCode' AND object_id = OBJECT_ID('Staff'))
                    CREATE UNIQUE INDEX [UQ_Staff_Tenant_Branch_EmployeeCode] ON [Staff] ([TenantId], [BranchId], [EmployeeCode]) WHERE ([EmployeeCode] IS NOT NULL);
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StaffAttendances_Branch_Date' AND object_id = OBJECT_ID('StaffAttendances'))
                    CREATE INDEX [IX_StaffAttendances_Branch_Date] ON [StaffAttendances] ([TenantId], [BranchId], [AttendanceDate]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StaffAttendances_ShiftId' AND object_id = OBJECT_ID('StaffAttendances'))
                    CREATE INDEX [IX_StaffAttendances_ShiftId] ON [StaffAttendances] ([ShiftId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StaffAttendances_StaffId' AND object_id = OBJECT_ID('StaffAttendances'))
                    CREATE INDEX [IX_StaffAttendances_StaffId] ON [StaffAttendances] ([StaffId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_StaffAttendances_Staff_Date' AND object_id = OBJECT_ID('StaffAttendances'))
                    CREATE UNIQUE INDEX [UQ_StaffAttendances_Staff_Date] ON [StaffAttendances] ([TenantId], [BranchId], [StaffId], [AttendanceDate]);
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StaffDocuments_Staff' AND object_id = OBJECT_ID('StaffDocuments'))
                    CREATE INDEX [IX_StaffDocuments_Staff] ON [StaffDocuments] ([TenantId], [BranchId], [StaffId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StaffDocuments_StaffId' AND object_id = OBJECT_ID('StaffDocuments'))
                    CREATE INDEX [IX_StaffDocuments_StaffId] ON [StaffDocuments] ([StaffId]);
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StaffLeaves_Branch_StartDate' AND object_id = OBJECT_ID('StaffLeaves'))
                    CREATE INDEX [IX_StaffLeaves_Branch_StartDate] ON [StaffLeaves] ([TenantId], [BranchId], [StartDate]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StaffLeaves_Staff' AND object_id = OBJECT_ID('StaffLeaves'))
                    CREATE INDEX [IX_StaffLeaves_Staff] ON [StaffLeaves] ([TenantId], [BranchId], [StaffId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StaffLeaves_StaffId' AND object_id = OBJECT_ID('StaffLeaves'))
                    CREATE INDEX [IX_StaffLeaves_StaffId] ON [StaffLeaves] ([StaffId]);
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StaffShifts_Staff_Active' AND object_id = OBJECT_ID('StaffShifts'))
                    CREATE INDEX [IX_StaffShifts_Staff_Active] ON [StaffShifts] ([TenantId], [BranchId], [StaffId], [IsActive]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StaffShifts_StaffId' AND object_id = OBJECT_ID('StaffShifts'))
                    CREATE INDEX [IX_StaffShifts_StaffId] ON [StaffShifts] ([StaffId]);
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Staff_Branches_Tenant' AND parent_object_id = OBJECT_ID('Staff'))
                    ALTER TABLE [Staff] WITH NOCHECK ADD CONSTRAINT [FK_Staff_Branches_Tenant] FOREIGN KEY ([TenantId], [BranchId]) REFERENCES [Branches] ([TenantId], [Id]);
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Staff_Users' AND parent_object_id = OBJECT_ID('Staff'))
                    ALTER TABLE [Staff] WITH NOCHECK ADD CONSTRAINT [FK_Staff_Users] FOREIGN KEY ([LinkedUserId]) REFERENCES [Users] ([Id]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Staff_Branches_Tenant' AND parent_object_id = OBJECT_ID('Staff'))
                    ALTER TABLE [Staff] DROP CONSTRAINT [FK_Staff_Branches_Tenant];
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Staff_Users' AND parent_object_id = OBJECT_ID('Staff'))
                    ALTER TABLE [Staff] DROP CONSTRAINT [FK_Staff_Users];
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID('StaffAttendances', 'U') IS NOT NULL DROP TABLE [StaffAttendances];
                IF OBJECT_ID('StaffDocuments', 'U') IS NOT NULL DROP TABLE [StaffDocuments];
                IF OBJECT_ID('StaffLeaves', 'U') IS NOT NULL DROP TABLE [StaffLeaves];
                IF OBJECT_ID('StaffShifts', 'U') IS NOT NULL DROP TABLE [StaffShifts];
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Staff_LinkedUserId' AND object_id = OBJECT_ID('Staff'))
                    DROP INDEX [IX_Staff_LinkedUserId] ON [Staff];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Staff_Tenant_Branch_Active' AND object_id = OBJECT_ID('Staff'))
                    DROP INDEX [IX_Staff_Tenant_Branch_Active] ON [Staff];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Staff_Tenant_Branch_Bookable' AND object_id = OBJECT_ID('Staff'))
                    DROP INDEX [IX_Staff_Tenant_Branch_Bookable] ON [Staff];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Staff_Tenant_Branch_EmployeeCode' AND object_id = OBJECT_ID('Staff'))
                    DROP INDEX [UQ_Staff_Tenant_Branch_EmployeeCode] ON [Staff];
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'PermissionsJson')
                    ALTER TABLE [Users] DROP COLUMN [PermissionsJson];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'BaseSalary')
                    ALTER TABLE [Staff] DROP COLUMN [BaseSalary];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'BranchId')
                    ALTER TABLE [Staff] DROP COLUMN [BranchId];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'DeductionPerAbsentDay')
                    ALTER TABLE [Staff] DROP COLUMN [DeductionPerAbsentDay];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'DeductionPerLateMinute')
                    ALTER TABLE [Staff] DROP COLUMN [DeductionPerLateMinute];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'Email')
                    ALTER TABLE [Staff] DROP COLUMN [Email];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'EmployeeCode')
                    ALTER TABLE [Staff] DROP COLUMN [EmployeeCode];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'EmploymentType')
                    ALTER TABLE [Staff] DROP COLUMN [EmploymentType];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'HireDate')
                    ALTER TABLE [Staff] DROP COLUMN [HireDate];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'IsBookableForAppointments')
                    ALTER TABLE [Staff] DROP COLUMN [IsBookableForAppointments];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'LinkedUserId')
                    ALTER TABLE [Staff] DROP COLUMN [LinkedUserId];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'Notes')
                    ALTER TABLE [Staff] DROP COLUMN [Notes];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'PhotoUrl')
                    ALTER TABLE [Staff] DROP COLUMN [PhotoUrl];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'SalaryType')
                    ALTER TABLE [Staff] DROP COLUMN [SalaryType];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'TenantId')
                    ALTER TABLE [Staff] DROP COLUMN [TenantId];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'TrackAttendance')
                    ALTER TABLE [Staff] DROP COLUMN [TrackAttendance];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Staff') AND name = 'WeeklyOffDays')
                    ALTER TABLE [Staff] DROP COLUMN [WeeklyOffDays];
                """);
        }
    }
}
