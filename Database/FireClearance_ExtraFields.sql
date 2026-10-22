/* ============================================================================
   Fire NOC — add the client-side detail fields and the extra document uploads
   ----------------------------------------------------------------------------
   Two idempotent ALTERs on the NewEODB database:

       dbo.FireClearanceApplications  - the extra Building / Fire-Safety /
                                        Previous-licence data fields that the
                                        citizen-side form now collects.

       dbo.FireClearanceDocuments     - the extra upload slots (one
                                        varbinary(max) column per uploaded
                                        file, same "one column per file"
                                        approach the table already uses).

   Safe to run more than once: every column is added only when missing.
   Run against NewEODB, e.g.:
       sqlcmd -S "192.168.1.13,1433\SQLEXPRESS" -U pipdic -P pipdic@123 -d NewEODB -i FireClearance_ExtraFields.sql
   ============================================================================ */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/* ----------------------------------------------------------------------------
   1. Extra data fields on the application table
   ---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.FireClearanceApplications', 'U') IS NOT NULL
BEGIN
    /* ---- Step 1: Premises details (extra) ---- */
    IF COL_LENGTH('dbo.FireClearanceApplications', 'premisesname') IS NULL
        ALTER TABLE dbo.FireClearanceApplications ADD [premisesname] NVARCHAR(200) NULL;
    IF COL_LENGTH('dbo.FireClearanceApplications', 'registrationtype') IS NULL
        ALTER TABLE dbo.FireClearanceApplications ADD [registrationtype] NVARCHAR(20) NULL;

    /* ---- Step 2: Building Details (extra) ---- */
    IF COL_LENGTH('dbo.FireClearanceApplications', 'yearofconstruction') IS NULL
        ALTER TABLE dbo.FireClearanceApplications ADD [yearofconstruction] INT NULL;
    IF COL_LENGTH('dbo.FireClearanceApplications', 'typeofconstruction') IS NULL
        ALTER TABLE dbo.FireClearanceApplications ADD [typeofconstruction] NVARCHAR(50) NULL;
    IF COL_LENGTH('dbo.FireClearanceApplications', 'builtuparea') IS NULL
        ALTER TABLE dbo.FireClearanceApplications ADD [builtuparea] NUMERIC(18,2) NULL;

    /* ---- Step 4: Fire fighting system / means of escape / other ---- */
    IF COL_LENGTH('dbo.FireClearanceApplications', 'fireextinguishers') IS NULL
        ALTER TABLE dbo.FireClearanceApplications ADD [fireextinguishers] NVARCHAR(20) NULL;
    IF COL_LENGTH('dbo.FireClearanceApplications', 'firehydrant') IS NULL
        ALTER TABLE dbo.FireClearanceApplications ADD [firehydrant] NVARCHAR(20) NULL;
    IF COL_LENGTH('dbo.FireClearanceApplications', 'firesprinkler') IS NULL
        ALTER TABLE dbo.FireClearanceApplications ADD [firesprinkler] NVARCHAR(20) NULL;
    IF COL_LENGTH('dbo.FireClearanceApplications', 'firealarm') IS NULL
        ALTER TABLE dbo.FireClearanceApplications ADD [firealarm] NVARCHAR(20) NULL;
    IF COL_LENGTH('dbo.FireClearanceApplications', 'emergencyexit') IS NULL
        ALTER TABLE dbo.FireClearanceApplications ADD [emergencyexit] NVARCHAR(20) NULL;
    IF COL_LENGTH('dbo.FireClearanceApplications', 'fireexit') IS NULL
        ALTER TABLE dbo.FireClearanceApplications ADD [fireexit] NVARCHAR(20) NULL;
    IF COL_LENGTH('dbo.FireClearanceApplications', 'lift') IS NULL
        ALTER TABLE dbo.FireClearanceApplications ADD [lift] NVARCHAR(20) NULL;
    IF COL_LENGTH('dbo.FireClearanceApplications', 'generator') IS NULL
        ALTER TABLE dbo.FireClearanceApplications ADD [generator] NVARCHAR(20) NULL;
    IF COL_LENGTH('dbo.FireClearanceApplications', 'transformer') IS NULL
        ALTER TABLE dbo.FireClearanceApplications ADD [transformer] NVARCHAR(20) NULL;
    IF COL_LENGTH('dbo.FireClearanceApplications', 'licenseno') IS NULL
        ALTER TABLE dbo.FireClearanceApplications ADD [licenseno] NVARCHAR(100) NULL;
    IF COL_LENGTH('dbo.FireClearanceApplications', 'licensedate') IS NULL
        ALTER TABLE dbo.FireClearanceApplications ADD [licensedate] DATE NULL;
END
GO

/* ----------------------------------------------------------------------------
   1b. Widen two Step 3 columns
   ----------------------------------------------------------------------------
   The department's form asks for the *details* of the fire escape and of the
   canteen / LPG cooking facility, not a Yes/No answer, so both columns need to
   hold a paragraph.
   ---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.FireClearanceApplications', 'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns c
               JOIN sys.types t ON t.user_type_id = c.user_type_id
               WHERE c.object_id = OBJECT_ID('dbo.FireClearanceApplications')
                 AND c.name = 'fireescape'
                 AND (t.name <> 'nvarchar' OR c.max_length < 800))
        ALTER TABLE dbo.FireClearanceApplications ALTER COLUMN [fireescape] NVARCHAR(400) NULL;

    IF EXISTS (SELECT 1 FROM sys.columns c
               JOIN sys.types t ON t.user_type_id = c.user_type_id
               WHERE c.object_id = OBJECT_ID('dbo.FireClearanceApplications')
                 AND c.name = 'canteen'
                 AND (t.name <> 'nvarchar' OR c.max_length < 800))
        ALTER TABLE dbo.FireClearanceApplications ALTER COLUMN [canteen] NVARCHAR(400) NULL;
END
GO

/* ----------------------------------------------------------------------------
   2. Extra upload slots on the document table
   ---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.FireClearanceDocuments', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.FireClearanceDocuments', 'siteplancdoc') IS NULL
        ALTER TABLE dbo.FireClearanceDocuments ADD [siteplancdoc] VARBINARY(MAX) NULL;   -- Site Plan / Layout Plan
    IF COL_LENGTH('dbo.FireClearanceDocuments', 'occupancycertdoc') IS NULL
        ALTER TABLE dbo.FireClearanceDocuments ADD [occupancycertdoc] VARBINARY(MAX) NULL; -- Occupancy Certificate
    IF COL_LENGTH('dbo.FireClearanceDocuments', 'otherauthoritydoc') IS NULL
        ALTER TABLE dbo.FireClearanceDocuments ADD [otherauthoritydoc] VARBINARY(MAX) NULL; -- NOC from other authorities
    IF COL_LENGTH('dbo.FireClearanceDocuments', 'fireauditreportdoc') IS NULL
        ALTER TABLE dbo.FireClearanceDocuments ADD [fireauditreportdoc] VARBINARY(MAX) NULL; -- Third-party fire safety audit report
    IF COL_LENGTH('dbo.FireClearanceDocuments', 'electricalnocdoc') IS NULL
        ALTER TABLE dbo.FireClearanceDocuments ADD [electricalnocdoc] VARBINARY(MAX) NULL; -- Electrical Inspector NOC
    IF COL_LENGTH('dbo.FireClearanceDocuments', 'liftcertdoc') IS NULL
        ALTER TABLE dbo.FireClearanceDocuments ADD [liftcertdoc] VARBINARY(MAX) NULL;   -- Lift / elevator inspection certificate
    IF COL_LENGTH('dbo.FireClearanceDocuments', 'firetrainingdoc') IS NULL
        ALTER TABLE dbo.FireClearanceDocuments ADD [firetrainingdoc] VARBINARY(MAX) NULL; -- Fire drill / training record
    IF COL_LENGTH('dbo.FireClearanceDocuments', 'hazardlicencedoc') IS NULL
        ALTER TABLE dbo.FireClearanceDocuments ADD [hazardlicencedoc] VARBINARY(MAX) NULL; -- Hazardous material licence
    IF COL_LENGTH('dbo.FireClearanceDocuments', 'taxreceiptdoc') IS NULL
        ALTER TABLE dbo.FireClearanceDocuments ADD [taxreceiptdoc] VARBINARY(MAX) NULL;  -- Property tax receipt
    IF COL_LENGTH('dbo.FireClearanceDocuments', 'idproofdoc') IS NULL
        ALTER TABLE dbo.FireClearanceDocuments ADD [idproofdoc] VARBINARY(MAX) NULL;     -- ID proof of applicant / owner
    IF COL_LENGTH('dbo.FireClearanceDocuments', 'sitephotodoc') IS NULL
        ALTER TABLE dbo.FireClearanceDocuments ADD [sitephotodoc] VARBINARY(MAX) NULL;   -- Photographs of the building / site
    IF COL_LENGTH('dbo.FireClearanceDocuments', 'staffstrengthdoc') IS NULL
        ALTER TABLE dbo.FireClearanceDocuments ADD [staffstrengthdoc] VARBINARY(MAX) NULL; -- Staff and student strength details
END
GO

PRINT 'Fire NOC extra fields + document upload slots are ready.';
GO

/* ----------------------------------------------------------------------------
   4. Column widths that must match the control used on the form
   ----------------------------------------------------------------------------
   "Total No. of Staff / Student / Workers in the Building" is a text area in
   the department's form (it holds free text, not a single number), so the
   column is widened from 50 to 200 characters. Idempotent.
   ---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.FireClearanceApplications', 'U') IS NOT NULL
   AND (SELECT CHARACTER_MAXIMUM_LENGTH
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME   = 'FireClearanceApplications'
          AND COLUMN_NAME  = 'staff') < 200
BEGIN
    ALTER TABLE dbo.FireClearanceApplications ALTER COLUMN [staff] NVARCHAR(200) NULL;
END
GO
