-- ============================================================
-- Resource Runner - Executes SQL files and logs to Resources table
-- wwwroot/resources/03-resource-runner.sql
-- This proc loads and executes .sql files from wwwroot/resources
-- If error: logs error to Resources.ErrorMessage
-- ============================================================

USE [PortfolioDB];
GO

-- Stored procedure to execute a resource file and log the result
IF OBJECT_ID('dbo.usp_ExecuteResourceFile', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_ExecuteResourceFile;
GO

CREATE PROCEDURE dbo.usp_ExecuteResourceFile
    @FileName     NVARCHAR(500),
    @FileContent  NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT OFF;

    DECLARE @ResourceId INT;
    DECLARE @ErrorMessage NVARCHAR(MAX) = NULL;
    DECLARE @IsSuccess BIT = 1;

    BEGIN TRY
        -- Execute the SQL content dynamically
        EXEC sp_executesql @FileContent;

        -- Record success
        IF EXISTS (SELECT 1 FROM [dbo].[Resources] WHERE [FileName] = @FileName)
        BEGIN
            UPDATE [dbo].[Resources]
            SET [FileContent]  = @FileContent,
                [ExecutedAt]   = SYSUTCDATETIME(),
                [IsSuccess]    = 1,
                [ErrorMessage] = NULL,
                [UpdatedAt]    = SYSUTCDATETIME()
            WHERE [FileName] = @FileName;
        END
        ELSE
        BEGIN
            INSERT INTO [dbo].[Resources] ([FileName], [FileContent], [IsSuccess], [ErrorMessage])
            VALUES (@FileName, @FileContent, 1, NULL);
        END
    END TRY
    BEGIN CATCH
        SET @IsSuccess = 0;
        SET @ErrorMessage = CONCAT(
            'ERROR_NUMBER: ', ERROR_NUMBER(), CHAR(13), CHAR(10),
            'ERROR_SEVERITY: ', ERROR_SEVERITY(), CHAR(13), CHAR(10),
            'ERROR_STATE: ', ERROR_STATE(), CHAR(13), CHAR(10),
            'ERROR_PROCEDURE: ', ISNULL(ERROR_PROCEDURE(), 'N/A'), CHAR(13), CHAR(10),
            'ERROR_LINE: ', ERROR_LINE(), CHAR(13), CHAR(10),
            'ERROR_MESSAGE: ', ERROR_MESSAGE()
        );

        -- Record failure with error details
        IF EXISTS (SELECT 1 FROM [dbo].[Resources] WHERE [FileName] = @FileName)
        BEGIN
            UPDATE [dbo].[Resources]
            SET [FileContent]  = @FileContent,
                [ExecutedAt]   = SYSUTCDATETIME(),
                [IsSuccess]    = 0,
                [ErrorMessage] = @ErrorMessage,
                [UpdatedAt]    = SYSUTCDATETIME()
            WHERE [FileName] = @FileName;
        END
        ELSE
        BEGIN
            INSERT INTO [dbo].[Resources] ([FileName], [FileContent], [IsSuccess], [ErrorMessage])
            VALUES (@FileName, @FileContent, 0, @ErrorMessage);
        END
    END CATCH;
END
GO

-- ============================================================
-- Bulk resource runner: executes all .sql files in order
-- ============================================================
IF OBJECT_ID('dbo.usp_RunAllResources', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_RunAllResources;
GO

CREATE PROCEDURE dbo.usp_RunAllResources
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FileList TABLE (
        Seq         INT IDENTITY(1,1),
        FileName    NVARCHAR(500),
        FileContent NVARCHAR(MAX)
    );

    -- Insert resources to execute (in dependency order)
    -- In production, these would be loaded from the file system or embedded resources
    -- This is called from the BlazorService API which reads files from wwwroot/resources

    DECLARE @CurrentFile NVARCHAR(500);
    DECLARE @CurrentContent NVARCHAR(MAX);
    DECLARE @Seq INT = 1;
    DECLARE @MaxSeq INT;

    -- Cursor through registered files
    DECLARE resource_cursor CURSOR FOR
        SELECT FileName, FileContent FROM @FileList ORDER BY Seq;

    OPEN resource_cursor;
    FETCH NEXT FROM resource_cursor INTO @CurrentFile, @CurrentContent;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC dbo.usp_ExecuteResourceFile @CurrentFile, @CurrentContent;
        FETCH NEXT FROM resource_cursor INTO @CurrentFile, @CurrentContent;
    END

    CLOSE resource_cursor;
    DEALLOCATE resource_cursor;

    -- Return summary
    SELECT
        [FileName],
        [ExecutedAt],
        [IsSuccess],
        CASE WHEN [IsSuccess] = 1 THEN 'OK' ELSE LEFT([ErrorMessage], 500) END AS [Status]
    FROM [dbo].[Resources]
    ORDER BY [ExecutedAt] DESC;
END
GO

PRINT 'Resource runner procedures created.';
GO
