/* ============================================================================
   Firms - Request for Name/Business Place/Partners
   Table: dbo.FirmNameChangeRequest
   ----------------------------------------------------------------------------
   Backs the Name Change of Firms service (FirmNameChangeController /
   Views/FirmNameChange).

   Columns
        RequestId        INT IDENTITY  PK
        IsNameChange     BIT           NOT NULL   \
        IsAddressChange  BIT           NOT NULL    > "Requested Change For"
        IsPartnership    BIT           NOT NULL   /
        IsOthers         BIT           NOT NULL
        FormFileName     NVARCHAR(520) NOT NULL   -- original name of uploaded form
        FormFilePath     NVARCHAR(1000) NOT NULL  -- /uploads/firmnamechange/<guid>.<ext>
        FormContentType  VARCHAR(100)  NULL
        FormFileSizeKB   INT           NULL
        ApplicationNo    VARCHAR(50)   NOT NULL
        ChallanNo        VARCHAR(50)   NOT NULL
        ChallanDate      DATETIME2(0)  NULL
        Amount           DECIMAL(18,2) NULL
        Remarks          NVARCHAR(2000) NOT NULL
        RequestStatus    VARCHAR(20)   NOT NULL   -- 'Submitted'
        CreatedBy        VARCHAR(100)  NULL
        CreatedOn        DATETIME2     NOT NULL
        ModifiedBy       VARCHAR(100)  NULL
        ModifiedOn       DATETIME2     NULL
        IsActive         BIT           NOT NULL

   Idempotent:
      * missing       -> table is created with the final schema
      * exists        -> IsMemberChange is renamed to IsPartnership, missing
                         challan/amount columns are added, and the table is
                         upgraded in place if RequestId is not IDENTITY or the
                         primary key is missing. Existing rows are preserved.

   Run against NewEODB:
        sqlcmd -S "10.65.11.152,1433" -U pipdic -P pipdic@123 -d NewEODB -C -i FirmNameChange_Tables.sql
   ============================================================================ */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/* ----------------------------------------------------------------------------
   1. Create the table when it does not exist yet
   ---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.FirmNameChangeRequest', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FirmNameChangeRequest
    (
        RequestId        INT IDENTITY(1,1) NOT NULL
                         CONSTRAINT PK_FirmNameChangeRequest PRIMARY KEY,
        IsNameChange     BIT            NOT NULL CONSTRAINT DF_FirmNameChangeReq_IsNameChange    DEFAULT (0),
        IsAddressChange  BIT            NOT NULL CONSTRAINT DF_FirmNameChangeReq_IsAddressChange DEFAULT (0),
        IsPartnership    BIT            NOT NULL CONSTRAINT DF_FirmNameChangeReq_IsPartnership   DEFAULT (0),
        IsOthers         BIT            NOT NULL CONSTRAINT DF_FirmNameChangeReq_IsOthers        DEFAULT (0),
        FormFileName     NVARCHAR(520)  NOT NULL CONSTRAINT DF_FirmNameChangeReq_FileName        DEFAULT (''),
        FormFilePath     NVARCHAR(1000) NOT NULL CONSTRAINT DF_FirmNameChangeReq_FilePath        DEFAULT (''),
        FormContentType  VARCHAR(100)   NULL,
        FormFileSizeKB   INT            NULL,
        ApplicationNo    VARCHAR(50)    NOT NULL CONSTRAINT DF_FirmNameChangeReq_ApplicationNo   DEFAULT (''),
        ChallanNo        VARCHAR(50)    NOT NULL CONSTRAINT DF_FirmNameChangeReq_ChallanNo       DEFAULT (''),
        ChallanDate      DATETIME2(0)   NULL,
        Amount           DECIMAL(18,2)  NULL,
        Remarks          NVARCHAR(2000) NOT NULL CONSTRAINT DF_FirmNameChangeReq_Remarks         DEFAULT (''),
        RequestStatus    VARCHAR(20)    NOT NULL CONSTRAINT DF_FirmNameChangeReq_Status          DEFAULT ('Submitted'),
        CreatedBy        VARCHAR(100)   NULL,
        CreatedOn        DATETIME2(0)   NOT NULL CONSTRAINT DF_FirmNameChangeReq_CreatedOn       DEFAULT (SYSDATETIME()),
        ModifiedBy       VARCHAR(100)   NULL,
        ModifiedOn       DATETIME2(0)   NULL,
        IsActive         BIT            NOT NULL CONSTRAINT DF_FirmNameChangeReq_IsActive        DEFAULT (1)
    );
END
GO

/* ----------------------------------------------------------------------------
   2. Normalise the columns on an existing table
   ---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.FirmNameChangeRequest', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.FirmNameChangeRequest', 'IsMemberChange') IS NOT NULL
       AND COL_LENGTH('dbo.FirmNameChangeRequest', 'IsPartnership') IS NULL
        EXEC sp_rename 'dbo.FirmNameChangeRequest.IsMemberChange', 'IsPartnership', 'COLUMN';

    IF COL_LENGTH('dbo.FirmNameChangeRequest', 'ChallanNo') IS NULL
        ALTER TABLE dbo.FirmNameChangeRequest
        ADD ChallanNo VARCHAR(50) NOT NULL CONSTRAINT DF_FirmNameChangeReq_ChallanNo DEFAULT ('');

    IF COL_LENGTH('dbo.FirmNameChangeRequest', 'ChallanDate') IS NULL
        ALTER TABLE dbo.FirmNameChangeRequest ADD ChallanDate DATETIME2(0) NULL;

    IF COL_LENGTH('dbo.FirmNameChangeRequest', 'Amount') IS NULL
        ALTER TABLE dbo.FirmNameChangeRequest ADD Amount DECIMAL(18,2) NULL;

    IF COL_LENGTH('dbo.FirmNameChangeRequest', 'ApplicationNo') IS NULL
        ALTER TABLE dbo.FirmNameChangeRequest
        ADD ApplicationNo VARCHAR(50) NOT NULL CONSTRAINT DF_FirmNameChangeReq_ApplicationNo DEFAULT ('');

    /* Serial No. belongs to the original Society clone and is not part of
       this service's form — retire it if it is still present. */
    IF COL_LENGTH('dbo.FirmNameChangeRequest', 'SerialNo') IS NOT NULL
        ALTER TABLE dbo.FirmNameChangeRequest DROP COLUMN SerialNo;
END
GO

/* ----------------------------------------------------------------------------
   3. Upgrade an existing table in place (identity / primary key)
   ---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.FirmNameChangeRequest', 'U') IS NOT NULL
   AND (
        COLUMNPROPERTY(OBJECT_ID('dbo.FirmNameChangeRequest'), 'RequestId', 'IsIdentity') = 0
        OR NOT EXISTS (SELECT 1 FROM sys.indexes
                       WHERE object_id = OBJECT_ID('dbo.FirmNameChangeRequest')
                         AND is_primary_key = 1)
   )
BEGIN
    SET NOCOUNT ON;

    DECLARE @cols NVARCHAR(MAX);

    SELECT @cols = STUFF((
        SELECT ', ' + QUOTENAME(c.name)
        FROM sys.columns c
        WHERE c.object_id = OBJECT_ID('dbo.FirmNameChangeRequest')
          AND c.name IN ('RequestId','IsNameChange','IsAddressChange','IsPartnership','IsOthers',
                         'FormFileName','FormFilePath','FormContentType','FormFileSizeKB',
                         'ApplicationNo','ChallanNo','ChallanDate','Amount','Remarks',
                         'RequestStatus','CreatedBy','CreatedOn','ModifiedBy','ModifiedOn','IsActive')
        ORDER BY c.column_id
        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '');

    BEGIN TRANSACTION;

    CREATE TABLE dbo.FirmNameChangeRequest_Tmp
    (
        RequestId        INT IDENTITY(1,1) NOT NULL
                         CONSTRAINT PK_FirmNameChangeRequest PRIMARY KEY,
        IsNameChange     BIT            NOT NULL CONSTRAINT DF_FirmNameChangeReq_IsNameChange    DEFAULT (0),
        IsAddressChange  BIT            NOT NULL CONSTRAINT DF_FirmNameChangeReq_IsAddressChange DEFAULT (0),
        IsPartnership    BIT            NOT NULL CONSTRAINT DF_FirmNameChangeReq_IsPartnership   DEFAULT (0),
        IsOthers         BIT            NOT NULL CONSTRAINT DF_FirmNameChangeReq_IsOthers        DEFAULT (0),
        FormFileName     NVARCHAR(520)  NOT NULL CONSTRAINT DF_FirmNameChangeReq_FileName        DEFAULT (''),
        FormFilePath     NVARCHAR(1000) NOT NULL CONSTRAINT DF_FirmNameChangeReq_FilePath        DEFAULT (''),
        FormContentType  VARCHAR(100)   NULL,
        FormFileSizeKB   INT            NULL,
        ApplicationNo    VARCHAR(50)    NOT NULL CONSTRAINT DF_FirmNameChangeReq_ApplicationNo   DEFAULT (''),
        ChallanNo        VARCHAR(50)    NOT NULL CONSTRAINT DF_FirmNameChangeReq_ChallanNo       DEFAULT (''),
        ChallanDate      DATETIME2(0)   NULL,
        Amount           DECIMAL(18,2)  NULL,
        Remarks          NVARCHAR(2000) NOT NULL CONSTRAINT DF_FirmNameChangeReq_Remarks         DEFAULT (''),
        RequestStatus    VARCHAR(20)    NOT NULL CONSTRAINT DF_FirmNameChangeReq_Status          DEFAULT ('Submitted'),
        CreatedBy        VARCHAR(100)   NULL,
        CreatedOn        DATETIME2(0)   NOT NULL CONSTRAINT DF_FirmNameChangeReq_CreatedOn       DEFAULT (SYSDATETIME()),
        ModifiedBy       VARCHAR(100)   NULL,
        ModifiedOn       DATETIME2(0)   NULL,
        IsActive         BIT            NOT NULL CONSTRAINT DF_FirmNameChangeReq_IsActive        DEFAULT (1)
    );

    IF @cols IS NOT NULL AND @cols <> ''
    BEGIN
        SET IDENTITY_INSERT dbo.FirmNameChangeRequest_Tmp ON;
        EXEC ('INSERT INTO dbo.FirmNameChangeRequest_Tmp (' + @cols + ') SELECT ' + @cols + ' FROM dbo.FirmNameChangeRequest;');
        SET IDENTITY_INSERT dbo.FirmNameChangeRequest_Tmp OFF;
    END

    DROP TABLE dbo.FirmNameChangeRequest;
    EXEC sp_rename 'dbo.FirmNameChangeRequest_Tmp', 'FirmNameChangeRequest';

    COMMIT TRANSACTION;
END
GO
