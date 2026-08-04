-- Add ErrorLog table to PortfolioDB
USE [PortfolioDB];
GO

IF OBJECT_ID('dbo.ErrorLogs','U') IS NULL
CREATE TABLE [dbo].[ErrorLogs](
    [Id]          INT IDENTITY(1,1)   NOT NULL,
    [Timestamp]   DATETIME2(7)        NOT NULL DEFAULT SYSUTCDATETIME(),
    [Level]       NVARCHAR(20)        NOT NULL DEFAULT 'Error',
    [Source]      NVARCHAR(300)       NULL,
    [Message]     NVARCHAR(MAX)       NOT NULL,
    [StackTrace]  NVARCHAR(MAX)       NULL,
    [RequestPath] NVARCHAR(500)       NULL,
    [Method]      NVARCHAR(10)        NULL,
    [StatusCode]  INT                 NULL,
    [UserId]      INT                 NULL,
    [UserAgent]   NVARCHAR(500)       NULL,
    [IpAddress]   NVARCHAR(50)        NULL,
    [IsResolved]  BIT                 NOT NULL DEFAULT 0,
    [ResolvedAt]  DATETIME2(7)        NULL,
    [ResolvedBy]  NVARCHAR(200)       NULL,
    [CreatedAt]   DATETIME2(7)        NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_ErrorLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE INDEX [IX_ErrorLogs_Timestamp] ON [dbo].[ErrorLogs]([Timestamp] DESC);
CREATE INDEX [IX_ErrorLogs_Unresolved] ON [dbo].[ErrorLogs]([IsResolved]) WHERE [IsResolved]=0;
GO

PRINT 'ErrorLogs table created.';
GO
