/* ============================================================================
   Name Availability — ADMIN module
   ----------------------------------------------------------------------------
   1. Adds the review/approval columns used by NameAvailabilityAdminController
      to BOTH tables (each service keeps its own table):
          dbo.SocietyNameAvailabilities
          dbo.FirmNameAvailabilities
      New columns:
          Status         VARCHAR(20)  NOT NULL  DEFAULT 'Pending'
          ApprovedName   NVARCHAR(200) NULL
          AdminRemarks   NVARCHAR(500) NULL
          ApprovedBy     VARCHAR(100)  NULL
          ApprovedOn     DATETIME2     NULL

   2. Creates the Name Availability administrator officer account:
          Username     : Society
          Password     : Society@123
          Designation  : Name Availability Admin
          Department   : Name Availability

      The password is stored as an ASP.NET Core Identity PasswordHasher hash
      (same hasher as every other officer account) — never plain text.

   Idempotent: safe to run more than once.
   Run against NewEODB:
       sqlcmd -S "192.168.1.13,1433\SQLEXPRESS" -U pipdic -P pipdic@123 -d NewEODB -i NameAvailabilityAdmin_Tables.sql
   ============================================================================ */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/* ----------------------------------------------------------------------------
   1a. Society table
   ---------------------------------------------------------------------------- */
IF COLUMNPROPERTY(OBJECT_ID('dbo.SocietyNameAvailabilities'), 'Status', 'ColumnId') IS NULL
    ALTER TABLE dbo.SocietyNameAvailabilities
        ADD Status VARCHAR(20) NOT NULL CONSTRAINT DF_SocNameAvail_Status DEFAULT ('Pending');
GO
IF COLUMNPROPERTY(OBJECT_ID('dbo.SocietyNameAvailabilities'), 'ApprovedName', 'ColumnId') IS NULL
    ALTER TABLE dbo.SocietyNameAvailabilities ADD ApprovedName NVARCHAR(200) NULL;
GO
IF COLUMNPROPERTY(OBJECT_ID('dbo.SocietyNameAvailabilities'), 'AdminRemarks', 'ColumnId') IS NULL
    ALTER TABLE dbo.SocietyNameAvailabilities ADD AdminRemarks NVARCHAR(500) NULL;
GO
IF COLUMNPROPERTY(OBJECT_ID('dbo.SocietyNameAvailabilities'), 'ApprovedBy', 'ColumnId') IS NULL
    ALTER TABLE dbo.SocietyNameAvailabilities ADD ApprovedBy VARCHAR(100) NULL;
GO
IF COLUMNPROPERTY(OBJECT_ID('dbo.SocietyNameAvailabilities'), 'ApprovedOn', 'ColumnId') IS NULL
    ALTER TABLE dbo.SocietyNameAvailabilities ADD ApprovedOn DATETIME2 NULL;
GO

/* ----------------------------------------------------------------------------
   1b. Firm table
   ---------------------------------------------------------------------------- */
IF COLUMNPROPERTY(OBJECT_ID('dbo.FirmNameAvailabilities'), 'Status', 'ColumnId') IS NULL
    ALTER TABLE dbo.FirmNameAvailabilities
        ADD Status VARCHAR(20) NOT NULL CONSTRAINT DF_FirmNameAvail_Status DEFAULT ('Pending');
GO
IF COLUMNPROPERTY(OBJECT_ID('dbo.FirmNameAvailabilities'), 'ApprovedName', 'ColumnId') IS NULL
    ALTER TABLE dbo.FirmNameAvailabilities ADD ApprovedName NVARCHAR(200) NULL;
GO
IF COLUMNPROPERTY(OBJECT_ID('dbo.FirmNameAvailabilities'), 'AdminRemarks', 'ColumnId') IS NULL
    ALTER TABLE dbo.FirmNameAvailabilities ADD AdminRemarks NVARCHAR(500) NULL;
GO
IF COLUMNPROPERTY(OBJECT_ID('dbo.FirmNameAvailabilities'), 'ApprovedBy', 'ColumnId') IS NULL
    ALTER TABLE dbo.FirmNameAvailabilities ADD ApprovedBy VARCHAR(100) NULL;
GO
IF COLUMNPROPERTY(OBJECT_ID('dbo.FirmNameAvailabilities'), 'ApprovedOn', 'ColumnId') IS NULL
    ALTER TABLE dbo.FirmNameAvailabilities ADD ApprovedOn DATETIME2 NULL;
GO

/* ----------------------------------------------------------------------------
   2. Administrator officer account (Username: Society / Password: Society@123)
      PasswordHasher<Officer> hash of "Society@123".
   ---------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Officers WHERE Username = 'Society')
BEGIN
    INSERT INTO dbo.Officers
        (Username, PasswordHash, FullName, Department, Designation, Email,
         IsLocked, FailedLoginAttempts, CreatedDate, CreatedBy)
    VALUES
        ('Society',
         'AQAAAAIAAYagAAAAEOnUMRcd9vHzDCGd+nwSrQ2fu4fLteF3FfXFsZSiO9Wl8ocHrXt05Sa7A/kU3xt5yg==',
         'Name Availability Administrator',
         'Name Availability',
         'Name Availability Admin',
         NULL,
         0, 0, SYSDATETIME(), 'system');
END
GO
