using Ayapos.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Ayapos.Api.Security;

public sealed class WorkforceSchemaBootstrapService
{
    private const string EmptyGuid = "00000000-0000-0000-0000-000000000000";

    private readonly AyaposDbContext _db;
    private readonly ILogger<WorkforceSchemaBootstrapService> _logger;

    public WorkforceSchemaBootstrapService(
        AyaposDbContext db,
        ILogger<WorkforceSchemaBootstrapService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        var sql = $"""
IF OBJECT_ID(N'[dbo].[Staff]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Staff](
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Staff_Id] DEFAULT (newid()),
        [TenantId] uniqueidentifier NOT NULL CONSTRAINT [DF_Staff_TenantId] DEFAULT ('{EmptyGuid}'),
        [BranchId] uniqueidentifier NOT NULL CONSTRAINT [DF_Staff_BranchId] DEFAULT ('{EmptyGuid}'),
        [LinkedUserId] uniqueidentifier NULL,
        [FullName] nvarchar(120) NOT NULL CONSTRAINT [DF_Staff_FullName] DEFAULT (N'Unnamed employee'),
        [Phone] nvarchar(30) NULL,
        [Email] nvarchar(120) NULL,
        [EmployeeCode] nvarchar(40) NULL,
        [JobTitle] nvarchar(40) NULL,
        [EmploymentType] nvarchar(30) NOT NULL CONSTRAINT [DF_Staff_EmploymentType] DEFAULT (N'employee'),
        [SalaryType] nvarchar(20) NOT NULL CONSTRAINT [DF_Staff_SalaryType] DEFAULT (N'monthly'),
        [BaseSalary] decimal(12,2) NULL,
        [DeductionPerLateMinute] decimal(12,2) NULL,
        [DeductionPerAbsentDay] decimal(12,2) NULL,
        [WeeklyOffDays] nvarchar(40) NULL,
        [HireDate] datetime2(0) NULL,
        [PhotoUrl] nvarchar(260) NULL,
        [Notes] nvarchar(500) NULL,
        [IsBookableForAppointments] bit NOT NULL CONSTRAINT [DF_Staff_IsBookableForAppointments] DEFAULT ((0)),
        [TrackAttendance] bit NOT NULL CONSTRAINT [DF_Staff_TrackAttendance] DEFAULT ((1)),
        [IsActive] bit NOT NULL CONSTRAINT [DF_Staff_IsActive] DEFAULT ((1)),
        [CreatedAt] datetime2(0) NOT NULL CONSTRAINT [DF_Staff_CreatedAt] DEFAULT (sysdatetime()),
        CONSTRAINT [PK_Staff] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END;

IF COL_LENGTH('dbo.Staff', 'TenantId') IS NULL
    ALTER TABLE [dbo].[Staff] ADD [TenantId] uniqueidentifier NOT NULL CONSTRAINT [DF_Staff_TenantId_Add] DEFAULT ('{EmptyGuid}');
IF COL_LENGTH('dbo.Staff', 'BranchId') IS NULL
    ALTER TABLE [dbo].[Staff] ADD [BranchId] uniqueidentifier NOT NULL CONSTRAINT [DF_Staff_BranchId_Add] DEFAULT ('{EmptyGuid}');
IF COL_LENGTH('dbo.Staff', 'LinkedUserId') IS NULL
    ALTER TABLE [dbo].[Staff] ADD [LinkedUserId] uniqueidentifier NULL;
IF COL_LENGTH('dbo.Staff', 'Email') IS NULL
    ALTER TABLE [dbo].[Staff] ADD [Email] nvarchar(120) NULL;
IF COL_LENGTH('dbo.Staff', 'EmployeeCode') IS NULL
    ALTER TABLE [dbo].[Staff] ADD [EmployeeCode] nvarchar(40) NULL;
IF COL_LENGTH('dbo.Staff', 'EmploymentType') IS NULL
    ALTER TABLE [dbo].[Staff] ADD [EmploymentType] nvarchar(30) NOT NULL CONSTRAINT [DF_Staff_EmploymentType_Add] DEFAULT (N'employee');
IF COL_LENGTH('dbo.Staff', 'SalaryType') IS NULL
    ALTER TABLE [dbo].[Staff] ADD [SalaryType] nvarchar(20) NOT NULL CONSTRAINT [DF_Staff_SalaryType_Add] DEFAULT (N'monthly');
IF COL_LENGTH('dbo.Staff', 'BaseSalary') IS NULL
    ALTER TABLE [dbo].[Staff] ADD [BaseSalary] decimal(12,2) NULL;
IF COL_LENGTH('dbo.Staff', 'DeductionPerLateMinute') IS NULL
    ALTER TABLE [dbo].[Staff] ADD [DeductionPerLateMinute] decimal(12,2) NULL;
IF COL_LENGTH('dbo.Staff', 'DeductionPerAbsentDay') IS NULL
    ALTER TABLE [dbo].[Staff] ADD [DeductionPerAbsentDay] decimal(12,2) NULL;
IF COL_LENGTH('dbo.Staff', 'WeeklyOffDays') IS NULL
    ALTER TABLE [dbo].[Staff] ADD [WeeklyOffDays] nvarchar(40) NULL;
IF COL_LENGTH('dbo.Staff', 'HireDate') IS NULL
    ALTER TABLE [dbo].[Staff] ADD [HireDate] datetime2(0) NULL;
IF COL_LENGTH('dbo.Staff', 'PhotoUrl') IS NULL
    ALTER TABLE [dbo].[Staff] ADD [PhotoUrl] nvarchar(260) NULL;
IF COL_LENGTH('dbo.Staff', 'Notes') IS NULL
    ALTER TABLE [dbo].[Staff] ADD [Notes] nvarchar(500) NULL;
IF COL_LENGTH('dbo.Staff', 'IsBookableForAppointments') IS NULL
    ALTER TABLE [dbo].[Staff] ADD [IsBookableForAppointments] bit NOT NULL CONSTRAINT [DF_Staff_IsBookableForAppointments_Add] DEFAULT ((0));
IF COL_LENGTH('dbo.Staff', 'TrackAttendance') IS NULL
    ALTER TABLE [dbo].[Staff] ADD [TrackAttendance] bit NOT NULL CONSTRAINT [DF_Staff_TrackAttendance_Add] DEFAULT ((1));

IF COL_LENGTH('dbo.Staff', 'TenantId') IS NOT NULL
   AND COL_LENGTH('dbo.Staff', 'BranchId') IS NOT NULL
   AND COL_LENGTH('dbo.Staff', 'IsActive') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Staff_Tenant_Branch_Active' AND object_id = OBJECT_ID(N'[dbo].[Staff]'))
BEGIN
    EXEC(N'CREATE INDEX [IX_Staff_Tenant_Branch_Active] ON [dbo].[Staff]([TenantId], [BranchId], [IsActive]);');
END;

IF COL_LENGTH('dbo.Staff', 'TenantId') IS NOT NULL
   AND COL_LENGTH('dbo.Staff', 'BranchId') IS NOT NULL
   AND COL_LENGTH('dbo.Staff', 'IsBookableForAppointments') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Staff_Tenant_Branch_Bookable' AND object_id = OBJECT_ID(N'[dbo].[Staff]'))
BEGIN
    EXEC(N'CREATE INDEX [IX_Staff_Tenant_Branch_Bookable] ON [dbo].[Staff]([TenantId], [BranchId], [IsBookableForAppointments]);');
END;

IF COL_LENGTH('dbo.Staff', 'TenantId') IS NOT NULL
   AND COL_LENGTH('dbo.Staff', 'BranchId') IS NOT NULL
   AND COL_LENGTH('dbo.Staff', 'EmployeeCode') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Staff_Tenant_Branch_EmployeeCode' AND object_id = OBJECT_ID(N'[dbo].[Staff]'))
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Staff_Tenant_Branch_EmployeeCode] ON [dbo].[Staff]([TenantId], [BranchId], [EmployeeCode]) WHERE [EmployeeCode] IS NOT NULL;');
END;

IF OBJECT_ID(N'[dbo].[StaffShifts]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[StaffShifts](
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_StaffShifts_Id] DEFAULT (newid()),
        [TenantId] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NOT NULL,
        [StaffId] uniqueidentifier NOT NULL,
        [Name] nvarchar(80) NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [GraceMinutes] int NOT NULL CONSTRAINT [DF_StaffShifts_GraceMinutes] DEFAULT ((0)),
        [IsActive] bit NOT NULL CONSTRAINT [DF_StaffShifts_IsActive] DEFAULT ((1)),
        [EffectiveFrom] datetime2(0) NULL,
        [EffectiveTo] datetime2(0) NULL,
        [WeeklyPattern] nvarchar(40) NULL,
        [CreatedAt] datetime2(0) NOT NULL CONSTRAINT [DF_StaffShifts_CreatedAt] DEFAULT (sysdatetime()),
        CONSTRAINT [PK_StaffShifts] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StaffShifts_Staff_Active' AND object_id = OBJECT_ID(N'[dbo].[StaffShifts]'))
    CREATE INDEX [IX_StaffShifts_Staff_Active] ON [dbo].[StaffShifts]([TenantId], [BranchId], [StaffId], [IsActive]);

IF OBJECT_ID(N'[dbo].[StaffAttendances]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[StaffAttendances](
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_StaffAttendances_Id] DEFAULT (newid()),
        [TenantId] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NOT NULL,
        [StaffId] uniqueidentifier NOT NULL,
        [ShiftId] uniqueidentifier NULL,
        [AttendanceDate] datetime2(0) NOT NULL,
        [CheckInAt] datetime2(0) NULL,
        [CheckOutAt] datetime2(0) NULL,
        [Status] nvarchar(20) NOT NULL CONSTRAINT [DF_StaffAttendances_Status] DEFAULT (N'present'),
        [LateMinutes] int NOT NULL CONSTRAINT [DF_StaffAttendances_LateMinutes] DEFAULT ((0)),
        [WorkedMinutes] int NOT NULL CONSTRAINT [DF_StaffAttendances_WorkedMinutes] DEFAULT ((0)),
        [DeductionAmount] decimal(12,2) NOT NULL CONSTRAINT [DF_StaffAttendances_DeductionAmount] DEFAULT ((0)),
        [Notes] nvarchar(400) NULL,
        [CreatedAt] datetime2(0) NOT NULL CONSTRAINT [DF_StaffAttendances_CreatedAt] DEFAULT (sysdatetime()),
        CONSTRAINT [PK_StaffAttendances] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StaffAttendances_Branch_Date' AND object_id = OBJECT_ID(N'[dbo].[StaffAttendances]'))
    CREATE INDEX [IX_StaffAttendances_Branch_Date] ON [dbo].[StaffAttendances]([TenantId], [BranchId], [AttendanceDate]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_StaffAttendances_Staff_Date' AND object_id = OBJECT_ID(N'[dbo].[StaffAttendances]'))
    CREATE UNIQUE INDEX [UQ_StaffAttendances_Staff_Date] ON [dbo].[StaffAttendances]([TenantId], [BranchId], [StaffId], [AttendanceDate]);

IF OBJECT_ID(N'[dbo].[StaffLeaves]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[StaffLeaves](
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_StaffLeaves_Id] DEFAULT (newid()),
        [TenantId] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NOT NULL,
        [StaffId] uniqueidentifier NOT NULL,
        [LeaveType] nvarchar(30) NOT NULL,
        [StartDate] datetime2(0) NOT NULL,
        [EndDate] datetime2(0) NOT NULL,
        [IsPaid] bit NOT NULL CONSTRAINT [DF_StaffLeaves_IsPaid] DEFAULT ((0)),
        [Status] nvarchar(20) NOT NULL CONSTRAINT [DF_StaffLeaves_Status] DEFAULT (N'approved'),
        [Notes] nvarchar(400) NULL,
        [CreatedAt] datetime2(0) NOT NULL CONSTRAINT [DF_StaffLeaves_CreatedAt] DEFAULT (sysdatetime()),
        CONSTRAINT [PK_StaffLeaves] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StaffLeaves_Branch_StartDate' AND object_id = OBJECT_ID(N'[dbo].[StaffLeaves]'))
    CREATE INDEX [IX_StaffLeaves_Branch_StartDate] ON [dbo].[StaffLeaves]([TenantId], [BranchId], [StartDate]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StaffLeaves_Staff' AND object_id = OBJECT_ID(N'[dbo].[StaffLeaves]'))
    CREATE INDEX [IX_StaffLeaves_Staff] ON [dbo].[StaffLeaves]([TenantId], [BranchId], [StaffId]);

IF OBJECT_ID(N'[dbo].[StaffDocuments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[StaffDocuments](
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_StaffDocuments_Id] DEFAULT (newid()),
        [TenantId] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NOT NULL,
        [StaffId] uniqueidentifier NOT NULL,
        [Title] nvarchar(120) NOT NULL,
        [DocumentType] nvarchar(40) NOT NULL,
        [FileName] nvarchar(160) NULL,
        [FileUrl] nvarchar(260) NULL,
        [ExpiresAt] datetime2(0) NULL,
        [CreatedAt] datetime2(0) NOT NULL CONSTRAINT [DF_StaffDocuments_CreatedAt] DEFAULT (sysdatetime()),
        CONSTRAINT [PK_StaffDocuments] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StaffDocuments_Staff' AND object_id = OBJECT_ID(N'[dbo].[StaffDocuments]'))
    CREATE INDEX [IX_StaffDocuments_Staff] ON [dbo].[StaffDocuments]([TenantId], [BranchId], [StaffId]);

IF COL_LENGTH('dbo.Users', 'PermissionsJson') IS NULL
    ALTER TABLE [dbo].[Users] ADD [PermissionsJson] nvarchar(max) NULL;
""";

        await _db.Database.ExecuteSqlRawAsync(sql, ct);
        _logger.LogInformation("Workforce schema bootstrap completed.");
    }
}
