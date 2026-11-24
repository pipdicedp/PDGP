/* ============================================================================
   Name Change of Society  —  table: dbo.SocietyNameChangeRequest
   ----------------------------------------------------------------------------
   Backs the "Society - Request for Name/Business Place/Members" service
   (SocietyNameChangeController / Views/SocietyNameChange).

   Columns
       RequestId        INT IDENTITY  PK
       ApplicationNo    VARCHAR(50)   NOT NULL
       SerialNo         VARCHAR(50)   NULL
       IsNameChange     BIT           NOT NULL   \
       IsAddressChange  BIT           NOT NULL    > "Requested Change For"
       IsMemberChange   BIT           NOT NULL   /
       IsOthers         BIT           NOT NULL
       FormFileName     NVARCHAR(520) NOT NULL   -- original name of uploaded form
       FormFilePath     NVARCHAR(1000) NOT NULL  -- /uploads/societynamechange/<guid>.<ext>
       FormContentType  VARCHAR(100)  NULL
       FormFileSizeKB   INT           NULL
       Remarks          NVARCHAR(2000) NOT NULL
       RequestStatus    VARCHAR(20)   NOT NULL   -- 'Submitted'
       CreatedBy        VARCHAR(100)  NULL
       CreatedOn        DATETIME2     NOT NULL
       ModifiedBy       VARCHAR(100)  NULL
       ModifiedOn       DATETIME2     NULL
       IsActive         BIT           NOT NULL

   Idempotent:
     * missing       -> table is created
     * exists        -> upgraded in place if RequestId is not IDENTITY,
                        SerialNo is missing, or the primary key is missing.
                        Existing rows are preserved (RequestId values kept).
     * obsolete dbo.SocietyNameChanges (abandoned first design) is dropped
       when it exists and is empty.

   Run against NewEODB:
       sqlcmd -S "192.168.1.13,1433\SQLEXPRESS" -U pipdic -P pipdic@123 -d NewEODB -i SocietyNameChange_Tables.sql
   ============================================================================ */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/* ----------------------------------------------------------------------------
   1. Create the table when it does not exist yet
   ---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.SocietyNameChangeRequest', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SocietyNameChangeRequest
    (
        RequestId        INT IDENTITY(1,1) NOT NULL
                         CONSTRAINT PK_SocietyNameChangeRequest PRIMARY KEY,
        ApplicationNo    VARCHAR(50)    NOT NULL,
        SerialNo         VARCHAR(50)    NULL,
        IsNameChange     BIT            NOT NULL CONSTRAINT DF_SocNameChangeReq_IsNameChange    DEFAULT (0),
        IsAddressChange  BIT            NOT NULL CONSTRAINT DF_SocNameChangeReq_IsAddressChange DEFAULT (0),
        IsMemberChange   BIT            NOT NULL CONSTRAINT DF_SocNameChangeReq_IsMemberChange  DEFAULT (0),
        IsOthers         BIT            NOT NULL CONSTRAINT DF_SocNameChangeReq_IsOthers        DEFAULT (0),
        FormFileName     NVARCHAR(520)  NOT NULL CONSTRAINT DF_SocNameChangeReq_FileName        DEFAULT (''),
        FormFilePath     NVARCHAR(1000) NOT NULL CONSTRAINT DF_SocNameChangeReq_FilePath        DEFAULT (''),
        FormContentType  VARCHAR(100)   NULL,
        FormFileSizeKB   INT            NULL,
        Remarks          NVARCHAR(2000) NOT NULL CONSTRAINT DF_SocNameChangeReq_Remarks         DEFAULT (''),
        RequestStatus    VARCHAR(20)    NOT NULL CONSTRAINT DF_SocNameChangeReq_Status          DEFAULT ('Submitted'),
        CreatedBy        VARCHAR(100)   NULL,
        CreatedOn        DATETIME2(0)   NOT NULL CONSTRAINT DF_SocNameChangeReq_CreatedOn       DEFAULT (SYSDATETIME()),
        ModifiedBy       VARCHAR(100)   NULL,
        ModifiedOn       DATETIME2(0)   NULL,
        IsActive         BIT            NOT NULL CONSTRAINT DF_SocNameChangeReq_IsActive        DEFAULT (1)
    );
END
GO

/* ----------------------------------------------------------------------------
   2. Upgrade an existing table in place (identity / SerialNo / primary key)
   ---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.SocietyNameChangeRequest', 'U') IS NOT NULL
   AND (
        COLUMNPROPERTY(OBJECT_ID('dbo.SocietyNameChangeRequest'), 'RequestId', 'IsIdentity') = 0
        OR COLUMNPROPERTY(OBJECT_ID('dbo.SocietyNameChangeRequest'), 'SerialNo', 'ColumnId') IS NULL
        OR NOT EXISTS (SELECT 1 FROM sys.indexes
                       WHERE object_id = OBJECT_ID('dbo.SocietyNameChangeRequest')
                         AND is_primary_key = 1)
   )
BEGIN
    SET NOCOUNT ON;

    DECLARE @cols NVARCHAR(MAX);

    /* Columns present in BOTH the old table and the target schema, so the copy
       works even if the table was created slightly differently. */
    SELECT @cols = STUFF((
        SELECT ', ' + QUOTENAME(c.name)
        FROM sys.columns c
        WHERE c.object_id = OBJECT_ID('dbo.SocietyNameChangeRequest')
          AND c.name IN ('RequestId','ApplicationNo','SerialNo','IsNameChange','IsAddressChange',
                         'IsMemberChange','IsOthers','FormFileName','FormFilePath','FormContentType',
                         'FormFileSizeKB','Remarks','RequestStatus','CreatedBy','CreatedOn',
                         'ModifiedBy','ModifiedOn','IsActive')
        ORDER BY c.column_id
        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '');

    BEGIN TRANSACTION;

    CREATE TABLE dbo.SocietyNameChangeRequest_Tmp
    (
        RequestId        INT IDENTITY(1,1) NOT NULL
                         CONSTRAINT PK_SocietyNameChangeRequest PRIMARY KEY,
        ApplicationNo    VARCHAR(50)    NOT NULL,
        SerialNo         VARCHAR(50)    NULL,
        IsNameChange     BIT            NOT NULL CONSTRAINT DF_SocNameChangeReq_IsNameChange    DEFAULT (0),
        IsAddressChange  BIT            NOT NULL CONSTRAINT DF_SocNameChangeReq_IsAddressChange DEFAULT (0),
        IsMemberChange   BIT            NOT NULL CONSTRAINT DF_SocNameChangeReq_IsMemberChange  DEFAULT (0),
        IsOthers         BIT            NOT NULL CONSTRAINT DF_SocNameChangeReq_IsOthers        DEFAULT (0),
        FormFileName     NVARCHAR(520)  NOT NULL CONSTRAINT DF_SocNameChangeReq_FileName        DEFAULT (''),
        FormFilePath     NVARCHAR(1000) NOT NULL CONSTRAINT DF_SocNameChangeReq_FilePath        DEFAULT (''),
        FormContentType  VARCHAR(100)   NULL,
        FormFileSizeKB   INT            NULL,
        Remarks          NVARCHAR(2000) NOT NULL CONSTRAINT DF_SocNameChangeReq_Remarks         DEFAULT (''),
        RequestStatus    VARCHAR(20)    NOT NULL CONSTRAINT DF_SocNameChangeReq_Status          DEFAULT ('Submitted'),
        CreatedBy        VARCHAR(100)   NULL,
        CreatedOn        DATETIME2(0)   NOT NULL CONSTRAINT DF_SocNameChangeReq_CreatedOn       DEFAULT (SYSDATETIME()),
        ModifiedBy       VARCHAR(100)   NULL,
        ModifiedOn       DATETIME2(0)   NULL,
        IsActive         BIT            NOT NULL CONSTRAINT DF_SocNameChangeReq_IsActive        DEFAULT (1)
    );

    IF @cols IS NOT NULL AND @cols <> ''
    BEGIN
        SET IDENTITY_INSERT dbo.SocietyNameChangeRequest_Tmp ON;
        EXEC ('INSERT INTO dbo.SocietyNameChangeRequest_Tmp (' + @cols + ') SELECT ' + @cols + ' FROM dbo.SocietyNameChangeRequest;');
        SET IDENTITY_INSERT dbo.SocietyNameChangeRequest_Tmp OFF;
    END

    DROP TABLE dbo.SocietyNameChangeRequest;
    EXEC sp_rename 'dbo.SocietyNameChangeRequest_Tmp', 'SocietyNameChangeRequest';

    COMMIT TRANSACTION;
END
GO

/* ----------------------------------------------------------------------------
   3. Drop the abandoned first-design table (only when it holds no rows)
   ---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.SocietyNameChanges', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.SocietyNameChanges)
BEGIN
    DROP TABLE dbo.SocietyNameChanges;
END
GO
