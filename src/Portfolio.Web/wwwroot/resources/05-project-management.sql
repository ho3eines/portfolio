USE [PortfolioDB];
GO

IF OBJECT_ID('dbo.Contracts','U') IS NULL
CREATE TABLE [dbo].[Contracts](
    [Id]            INT IDENTITY(1,1) NOT NULL,
    [Title]         NVARCHAR(300)    NOT NULL,
    [ClientName]    NVARCHAR(300)    NULL,
    [ClientEmail]   NVARCHAR(300)    NULL,
    [Amount]        DECIMAL(18,2)    NULL,
    [Currency]      NVARCHAR(10)     NULL DEFAULT 'EUR',
    [StartDate]     DATE             NULL,
    [EndDate]       DATE             NULL,
    [Status]        NVARCHAR(50)     NOT NULL DEFAULT 'Pending',
    [Description]   NVARCHAR(MAX)    NULL,
    [Notes]         NVARCHAR(MAX)    NULL,
    [CreatedAt]     DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]     DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Contracts] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

IF OBJECT_ID('dbo.ProjectPhases','U') IS NULL
CREATE TABLE [dbo].[ProjectPhases](
    [Id]            INT IDENTITY(1,1) NOT NULL,
    [ProjectId]     INT              NOT NULL,
    [Title]         NVARCHAR(300)    NOT NULL,
    [Description]   NVARCHAR(MAX)    NULL,
    [Status]        NVARCHAR(50)     NOT NULL DEFAULT 'Pending',
    [Progress]      INT              NOT NULL DEFAULT 0,
    [SortOrder]     INT              NOT NULL DEFAULT 0,
    [StartDate]     DATE             NULL,
    [EndDate]       DATE             NULL,
    [CompletedAt]   DATETIME2(7)     NULL,
    [CreatedAt]     DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]     DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_ProjectPhases] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ProjectPhases_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Projects]([Id]) ON DELETE CASCADE
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Projects') AND name='ContractId')
BEGIN
    ALTER TABLE [dbo].[Projects] ADD [ContractId] INT NULL;
    ALTER TABLE [dbo].[Projects] ADD CONSTRAINT [FK_Projects_Contracts] FOREIGN KEY ([ContractId]) REFERENCES [dbo].[Contracts]([Id]) ON DELETE SET NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Projects') AND name='ClientName')
    ALTER TABLE [dbo].[Projects] ADD [ClientName] NVARCHAR(300) NULL;

PRINT 'Contracts + ProjectPhases ready.';
GO
