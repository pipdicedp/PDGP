/* ============================================================================
   Fire NOC  /  Fire Safety Clearance  (deptcode = 9, serviceid = 1017)
   ----------------------------------------------------------------------------
   Creates the two tables the Fire NOC module needs in the NewEODB database:

       dbo.FireClearanceApplications  - one row per applicant (loginid = the
                                        citizen's UserId). Holds every field the
                                        applicant fills in on the 4 wizard steps
                                        PLUS the officer-workflow columns that
                                        FireClearanceApplication (the EF entity)
                                        maps.

       dbo.FireClearanceDocuments     - one row per applicant with one
                                        varbinary(max) column per uploaded
                                        document (same "one column per file"
                                        approach as dbo.caf_doc_table).

   Idempotent: safe to run more than once. Nothing is dropped.
   Run against NewEODB, e.g.:
       sqlcmd -S "192.168.1.13,1433\SQLEXPRESS" -U pipdic -P pipdic@123 -d NewEODB -i FireClearance_Schema.sql

   NOTE: this file is the "create from scratch" script. A database created
   before the extra Building / Fire-Safety / document fields were added must
   also run FireClearance_ExtraFields.sql (idempotent ALTERs that add the
   missing columns).
   ============================================================================ */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/* ----------------------------------------------------------------------------
   1. Main application table
   ---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.FireClearanceApplications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FireClearanceApplications
    (
        /* ---- keys / workflow (mapped by the EF entity) ---------------- */
        [Id]                 INT             IDENTITY(1,1) NOT NULL,
        [loginid]            BIGINT          NOT NULL,
        [userid]             INT             NULL,
        [appcode]            VARCHAR(30)     NULL,
        [status]             VARCHAR(30)     NOT NULL CONSTRAINT DF_FireApp_status       DEFAULT ('Draft'),
        [currentstage]       VARCHAR(50)     NOT NULL CONSTRAINT DF_FireApp_stage        DEFAULT ('Initial Scrutiny'),
        [assignedofficerid]  INT             NULL,
        [paymentstatus]      VARCHAR(30)     NOT NULL CONSTRAINT DF_FireApp_paystatus    DEFAULT ('NotRequested'),
        [officerremarks]     NVARCHAR(1000)  NULL,
        [modifieddate]       DATETIME2(0)    NULL,
        [submitteddate]      DATETIME2(0)    NULL,
        [serviceid]          INT             NULL,
        [servicedes]         NVARCHAR(150)   NULL,
        [deptcode]           SMALLINT        NULL,

        /* Denormalised copies of the applicant's name / mobile — kept only so
           the officer queue can show them without a second read. */
        [name]               NVARCHAR(150)   NULL,
        [mobileno]           NVARCHAR(20)    NULL,

        /* ---- Step 1: Applicant & Owner Details ---- */
        [premisesname]       NVARCHAR(200)   NULL,
        [registrationtype]   NVARCHAR(20)    NULL,
        [address]            NVARCHAR(400)   NULL,
        [email]              NVARCHAR(150)   NULL,
        [phno]               NVARCHAR(20)    NULL,
        [ownername]          NVARCHAR(150)   NULL,
        [owneraddress]       NVARCHAR(400)   NULL,

        /* ---- Step 2: Building Details ---- */
        [buildingclass]      NVARCHAR(50)    NULL,
        [floortype]          NVARCHAR(50)    NULL,
        [ht]                 NUMERIC(18,2)   NULL,
        [plotarea]           NUMERIC(18,2)   NULL,
        [plintharea]         NUMERIC(18,2)   NULL,
        [nooffloor]          INT             NULL,
        [areacovered]        NUMERIC(18,2)   NULL,
        [buildingstorage]    NVARCHAR(200)   NULL,
        [basementdetail]     NVARCHAR(400)   NULL,
        [yearofconstruction] INT             NULL,
        [typeofconstruction] NVARCHAR(50)    NULL,
        [builtuparea]        NUMERIC(18,2)   NULL,

        /* ---- Step 3: Occupancy & Business Details ---- */
        [occupencydetail]    NVARCHAR(400)   NULL,
        [muloccupencydetail] NVARCHAR(400)   NULL,
        [setbackdetail]      NVARCHAR(400)   NULL,
        [purpose]            NVARCHAR(200)   NULL,
        [businessnature]     NVARCHAR(200)   NULL,
        [staff]              NVARCHAR(50)    NULL,
        [hazard]             NVARCHAR(20)    NULL,
        [fireescape]         NVARCHAR(400)   NULL,
        [canteen]            NVARCHAR(400)   NULL,
        [noofbed]            NVARCHAR(50)    NULL,
        [noofrooms]          NVARCHAR(50)    NULL,

        /* ---- Step 4: Site & Fire Safety ---- */
        [surveyno]           NVARCHAR(50)    NULL,
        [distcode]           SMALLINT        NULL,
        [firefighting]       NVARCHAR(400)   NULL,
        [watersource]        NVARCHAR(200)   NULL,
        [firesought]         NVARCHAR(50)    NULL,

        /* ---- Step 4: installed fire-fighting systems (Yes / No) ---- */
        [fireextinguishers]  NVARCHAR(20)    NULL,
        [firehydrant]        NVARCHAR(20)    NULL,
        [firesprinkler]      NVARCHAR(20)    NULL,
        [firealarm]          NVARCHAR(20)    NULL,

        /* ---- Step 4: means of escape ---- */
        [emergencyexit]      NVARCHAR(20)    NULL,
        [fireexit]           NVARCHAR(20)    NULL,

        /* ---- Step 4: other equipment ---- */
        [lift]               NVARCHAR(20)    NULL,
        [generator]          NVARCHAR(20)    NULL,
        [transformer]        NVARCHAR(20)    NULL,

        /* ---- Step 4: previous Fire licence (renewals) ---- */
        [licenseno]          NVARCHAR(100)   NULL,
        [licensedate]        DATE            NULL,

        /* ---- service stamps ---- */
        [apptype]            NVARCHAR(20)    NULL,
        [appliedon]          DATE            NULL,

        CONSTRAINT [PK_FireClearanceApplications] PRIMARY KEY CLUSTERED ([Id])
    );
END
GO

/* One application per citizen (loginid = Users.UserId). */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_FireClearanceApplications_loginid'
               AND object_id = OBJECT_ID('dbo.FireClearanceApplications'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UX_FireClearanceApplications_loginid]
        ON dbo.FireClearanceApplications ([loginid]);
END
GO

/* Optional FK to Officers for the workflow assignment — matches the shared
   engine's "SetNull on officer delete" behaviour. Added defensively: only if
   both the table and the Officers table exist and the FK isn't already there. */
IF OBJECT_ID('dbo.Officers', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.FireClearanceApplications', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FireClearanceApplications_Officers_AssignedOfficerId')
BEGIN
    ALTER TABLE dbo.FireClearanceApplications WITH NOCHECK
        ADD CONSTRAINT [FK_FireClearanceApplications_Officers_AssignedOfficerId]
        FOREIGN KEY ([assignedofficerid]) REFERENCES dbo.Officers ([OfficerId])
        ON DELETE SET NULL;
END
GO

/* ============================================================================
   dbo.FireClearanceDocuments - one row per applicant, one varbinary(max)
   column per uploaded document.
   ============================================================================ */
IF OBJECT_ID('dbo.FireClearanceDocuments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FireClearanceDocuments
    (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [loginid]       BIGINT            NOT NULL,
        [appcode]       VARCHAR(30)       NULL,
        [uploadedon]    DATETIME2(0)      NULL,

        [buildingdoc]   VARBINARY(MAX)    NULL,   -- Building Plan
        [siteplancdoc]  VARBINARY(MAX)    NULL,   -- Site Plan / Layout Plan
        [landdoc]       VARBINARY(MAX)    NULL,   -- Land / Ownership Document
        [taxreceiptdoc] VARBINARY(MAX)    NULL,   -- Property Tax Receipt
        [idproofdoc]    VARBINARY(MAX)    NULL,   -- ID Proof of Applicant / Owner
        [occupancycertdoc] VARBINARY(MAX) NULL,   -- Occupancy Certificate
        [statutorydoc]  VARBINARY(MAX)    NULL,   -- Statutory Approval
        [otherauthoritydoc] VARBINARY(MAX) NULL,  -- NOC from Other Authorities
        [firedoc]       VARBINARY(MAX)    NULL,   -- Existing Fire Certificate
        [firesafetydoc] VARBINARY(MAX)    NULL,   -- Fire Safety Certificate
        [fireauditreportdoc] VARBINARY(MAX) NULL, -- Third Party Fire Safety Audit Report
        [firetrainingdoc] VARBINARY(MAX)  NULL,   -- Fire Drill / Training Record
        [stabilitydoc]  VARBINARY(MAX)    NULL,   -- Structural Stability Certificate
        [strengthdoc]   VARBINARY(MAX)    NULL,   -- Structural Strength Certificate
        [electricalnocdoc] VARBINARY(MAX) NULL,   -- Electrical Inspector NOC
        [liftcertdoc]   VARBINARY(MAX)    NULL,   -- Lift / Elevator Inspection Certificate
        [commodoc]      VARBINARY(MAX)    NULL,   -- Commodities / Goods Details
        [hazardlicencedoc] VARBINARY(MAX) NULL,   -- Hazardous Material Licence
        [schooldoc]     VARBINARY(MAX)    NULL,   -- School / Institution Certificate
        [staffstrengthdoc] VARBINARY(MAX) NULL,   -- Staff and Student Strength Details
        [insurancedoc]  VARBINARY(MAX)    NULL,   -- Insurance Document
        [sitephotodoc]  VARBINARY(MAX)    NULL,   -- Photographs of the Building / Site
        [othersdoc]     VARBINARY(MAX)    NULL,   -- Other Document

        CONSTRAINT [PK_FireClearanceDocuments] PRIMARY KEY CLUSTERED ([Id])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_FireClearanceDocuments_loginid'
               AND object_id = OBJECT_ID('dbo.FireClearanceDocuments'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UX_FireClearanceDocuments_loginid]
        ON dbo.FireClearanceDocuments ([loginid]);
END
GO

PRINT 'Fire Clearance schema ready (FireClearanceApplications, FireClearanceDocuments).';
GO
