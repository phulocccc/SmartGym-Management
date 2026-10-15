-- Run against the configured LocalDB instance, with the application stopped.
-- Deletes application and Identity data, including all accounts.
-- Keeps tables, indexes, identity definitions and EF migration history.
USE [SmartGymDb];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'SmartGymDb' OR ISNULL(CONVERT(int, SERVERPROPERTY('IsLocalDB')), 0) <> 1
    THROW 51001, 'Expected LocalDB database SmartGymDb. No data was deleted.', 1;
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
    THROW 51002, 'Migration history is missing. No data was deleted.', 1;
IF EXISTS (SELECT 1 FROM sys.tables WHERE is_ms_shipped = 0 AND temporal_type <> 0)
    THROW 51003, 'Temporal tables require a separate reset procedure. No data was deleted.', 1;
IF EXISTS (SELECT 1 FROM sys.triggers WHERE parent_class = 1 AND is_disabled = 0 AND is_ms_shipped = 0)
    THROW 51004, 'Enabled table triggers require review before reset. No data was deleted.', 1;

-- The repository already contains this backup directory. Backup must succeed first.
DECLARE @backupPath nvarchar(4000) =
    N'C:\Users\Admin\source\repos\SmartGymManagement\DatabaseBackups\SmartGymDb_before_clear_' +
    CONVERT(char(8), GETDATE(), 112) + N'_' +
    REPLACE(CONVERT(char(12), GETDATE(), 114), N':', N'') + N'.bak';
BACKUP DATABASE [SmartGymDb] TO DISK = @backupPath WITH COPY_ONLY, CHECKSUM;
RESTORE VERIFYONLY FROM DISK = @backupPath WITH CHECKSUM;
SELECT @backupPath AS BackupFile;

CREATE TABLE #Tables (TableName nvarchar(517) NOT NULL PRIMARY KEY);
CREATE TABLE #ForeignKeys
(
    TableName nvarchar(517) NOT NULL,
    ConstraintName nvarchar(258) NOT NULL,
    WasNotTrusted bit NOT NULL
);
CREATE TABLE #Counts (TableName nvarchar(517) NOT NULL, RemainingRows bigint NOT NULL);

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lockResult int;
    EXEC @lockResult = sys.sp_getapplock @Resource=N'SmartGym.Reservations',
        @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=15000;
    IF @lockResult < 0 THROW 51005, 'Could not acquire application lock.', 1;

    INSERT #Tables
    SELECT QUOTENAME(s.name) + N'.' + QUOTENAME(t.name)
    FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE t.is_ms_shipped = 0 AND t.object_id <> OBJECT_ID(N'dbo.__EFMigrationsHistory');

    -- Capture enabled foreign keys only. Disabled constraints remain disabled.
    INSERT #ForeignKeys
    SELECT QUOTENAME(s.name) + N'.' + QUOTENAME(t.name), QUOTENAME(f.name), f.is_not_trusted
    FROM sys.foreign_keys f JOIN sys.tables t ON t.object_id = f.parent_object_id
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE t.is_ms_shipped = 0 AND f.is_disabled = 0;

    DECLARE @sql nvarchar(max) = N'';
    SELECT @sql = @sql + N'ALTER TABLE ' + TableName + N' NOCHECK CONSTRAINT ' + ConstraintName + N';'
    FROM #ForeignKeys;
    IF LEN(@sql) > 0 EXEC sys.sp_executesql @sql;

    SET @sql = N'';
    SELECT @sql = @sql + N'DELETE FROM ' + TableName + N';'
    FROM #Tables;
    IF LEN(@sql) > 0 EXEC sys.sp_executesql @sql;

    -- Restore each foreign key's original enabled/trusted state.
    SET @sql = N'';
    SELECT @sql = @sql + N'ALTER TABLE ' + TableName +
        CASE WHEN WasNotTrusted = 0 THEN N' WITH CHECK CHECK CONSTRAINT ' ELSE N' WITH NOCHECK CHECK CONSTRAINT ' END +
        ConstraintName + N';'
    FROM #ForeignKeys;
    IF LEN(@sql) > 0 EXEC sys.sp_executesql @sql;

    SET @sql = N'';
    SELECT @sql = @sql + N'INSERT #Counts SELECT N''' + REPLACE(TableName, N'''', N'''''') +
        N''', COUNT_BIG(*) FROM ' + TableName + N';'
    FROM #Tables;
    IF LEN(@sql) > 0 EXEC sys.sp_executesql @sql;
    IF EXISTS (SELECT 1 FROM #Counts WHERE RemainingRows <> 0)
        THROW 51006, 'Some tables are not empty; reset rolled back.', 1;

    COMMIT TRANSACTION;
    SELECT TableName, RemainingRows FROM #Counts ORDER BY TableName;
    SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
    PRINT 'All application and Identity data deleted. Schema and migration history retained.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
