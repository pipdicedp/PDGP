-- CAF Industry officer review trail (one row per CAF).
-- Run once on every database that runs the feature/caf-officer code.
-- Safe to re-run: it only creates the table if it is missing.

IF OBJECT_ID(N'dbo.CafIndustryReviews', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CafIndustryReviews (
        Id                      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CafIndustryReviews PRIMARY KEY,
        LoginId                 BIGINT        NOT NULL,
        Status                  NVARCHAR(20)  NOT NULL CONSTRAINT DF_CafIndustryReviews_Status DEFAULT N'Viewed',
        FirstViewedDate         DATETIME2     NULL,
        FirstViewedByOfficerId  INT           NULL,
        LastActionDate          DATETIME2     NULL,
        LastActionByOfficerId   INT           NULL,
        Remarks                 NVARCHAR(1000) NULL
    );

    CREATE UNIQUE INDEX IX_CafIndustryReviews_LoginId ON dbo.CafIndustryReviews (LoginId);
END
GO
