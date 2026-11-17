/* ============================================================================
   Name Availability of SOCIETIES / FIRMS  —  two separate tables
   ----------------------------------------------------------------------------
   Creates the two tables used by the Name Availability services in the
   NewEODB database:

       dbo.SocietyNameAvailabilities  - Name Availability of Societies requests
                                         (NameAvailabilityController)
       dbo.FirmNameAvailabilities     - Name Availability of Firms requests
                                         (NameAvailabilityFirmController)

   Both share the same columns (see Models/NameAvailability.cs —
   NameAvailabilityBase), but rows never mix: each service writes only to
   its own table, so Society and Firm requests stay separate.

   Also drops the never-used tables of the abandoned first implementation
   (dbo.SocietyNameAvailabilityApplications / dbo.SocietyNameAvailabilityDocuments).

   NOTE: dbo.NameAvailabilities (the old shared table) is intentionally LEFT
   IN PLACE — no code uses it any more, so drop it manually once you are sure
   you don't need its rows (or copy them into SocietyNameAvailabilities first).

   Idempotent: safe to run more than once. Nothing in use is dropped.
   Run against NewEODB, e.g.:
       sqlcmd -S "192.168.1.13,1433\SQLEXPRESS" -U pipdic -P pipdic@123 -d NewEODB -i NameAvailability_Tables.sql
   ============================================================================ */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/* ----------------------------------------------------------------------------
   1. Society table
   ---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.SocietyNameAvailabilities', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SocietyNameAvailabilities
    (
        Id                     INT IDENTITY(1,1) PRIMARY KEY,
        ProposedName1          NVARCHAR(200)  NOT NULL,
        ProposedName2          NVARCHAR(200)  NULL,
        ProposedName3          NVARCHAR(200)  NULL,
        ProposedName4          NVARCHAR(200)  NULL,
        ProposedName5          NVARCHAR(200)  NULL,
        Purpose                NVARCHAR(1000) NULL,
        ApplicantName          NVARCHAR(150)  NOT NULL,
        ApplicantAddress       NVARCHAR(500)  NOT NULL,
        MobileNo               NVARCHAR(20)   NOT NULL,
        Email                  NVARCHAR(150)  NOT NULL,
        InConsonanceWithObjects BIT           NOT NULL,
        NocAttached            BIT            NOT NULL,
        EmblemsActCompliant    BIT            NOT NULL,
        IdentityProofPath      NVARCHAR(300)  NULL,
        SignatureProofPath     NVARCHAR(300)  NULL,
        NocPath                NVARCHAR(300)  NULL,
        CreatedOn              DATETIME       NOT NULL CONSTRAINT DF_SocNameAvail_CreatedOn DEFAULT GETDATE()
    );
END
GO

/* ----------------------------------------------------------------------------
   2. Firm table
   ---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.FirmNameAvailabilities', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FirmNameAvailabilities
    (
        Id                     INT IDENTITY(1,1) PRIMARY KEY,
        ProposedName1          NVARCHAR(200)  NOT NULL,
        ProposedName2          NVARCHAR(200)  NULL,
        ProposedName3          NVARCHAR(200)  NULL,
        ProposedName4          NVARCHAR(200)  NULL,
        ProposedName5          NVARCHAR(200)  NULL,
        Purpose                NVARCHAR(1000) NULL,
        ApplicantName          NVARCHAR(150)  NOT NULL,
        ApplicantAddress       NVARCHAR(500)  NOT NULL,
        MobileNo               NVARCHAR(20)   NOT NULL,
        Email                  NVARCHAR(150)  NOT NULL,
        InConsonanceWithObjects BIT           NOT NULL,
        NocAttached            BIT            NOT NULL,
        EmblemsActCompliant    BIT            NOT NULL,
        IdentityProofPath      NVARCHAR(300)  NULL,
        SignatureProofPath     NVARCHAR(300)  NULL,
        NocPath                NVARCHAR(300)  NULL,
        CreatedOn              DATETIME       NOT NULL CONSTRAINT DF_FirmNameAvail_CreatedOn DEFAULT GETDATE()
    );
END
GO

/* ----------------------------------------------------------------------------
   3. Drop the never-used tables of the abandoned workflow implementation
   ---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.SocietyNameAvailabilityDocuments', 'U') IS NOT NULL
    DROP TABLE dbo.SocietyNameAvailabilityDocuments;
GO
IF OBJECT_ID('dbo.SocietyNameAvailabilityApplications', 'U') IS NOT NULL
    DROP TABLE dbo.SocietyNameAvailabilityApplications;
GO
