/*
    VerifyApplicationDocumentsSchema.sql
    -------------------------------------
    The code now matches your Application_Documents table exactly as you
    defined it -- no extra columns needed. The only requirement is that
    File_Path is varbinary(max) (which you've already changed it to).

    Run this against NewEODB to confirm:
*/

USE [NewEODB]
GO

SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Application_Documents'
ORDER BY ORDINAL_POSITION;
GO

-- Expected: File_Path should show DATA_TYPE = 'varbinary'.
-- If it still shows 'varchar', run:
-- ALTER TABLE dbo.Application_Documents ALTER COLUMN File_Path varbinary(max) NULL;
