-- ============================================================
-- PortfolioDB — Complete Database Script
-- wwwroot/resources/01-create-database.sql
-- Database-First approach. Run first.
-- ============================================================
USE [master];
GO

IF DB_ID('PortfolioDB') IS NULL
BEGIN
    -- No FILENAME clauses → SQL Server uses its default data directory,
    -- so this works on both Windows and Linux (Docker) installations.
    CREATE DATABASE [PortfolioDB];
END
GO

USE [PortfolioDB];
GO

-- ============================================================
-- 1. USERS & AUTH
-- ============================================================
IF OBJECT_ID('dbo.Roles','U') IS NULL
CREATE TABLE [dbo].[Roles](
    [Id]          INT IDENTITY(1,1) NOT NULL,
    [Name]        NVARCHAR(100)    NOT NULL,
    [Description] NVARCHAR(500)    NULL,
    [CreatedAt]   DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Roles_Name] UNIQUE ([Name])
);

IF OBJECT_ID('dbo.Users','U') IS NULL
CREATE TABLE [dbo].[Users](
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [Username]        NVARCHAR(100)    NOT NULL,
    [Email]           NVARCHAR(300)    NOT NULL,
    [PasswordHash]    NVARCHAR(500)    NOT NULL,
    [FullName]        NVARCHAR(200)    NULL,
    [AvatarUrl]       NVARCHAR(500)    NULL,
    [Bio]             NVARCHAR(MAX)    NULL,
    [RoleId]          INT              NOT NULL DEFAULT 1,
    [IsActive]        BIT              NOT NULL DEFAULT 1,
    [LastLoginAt]     DATETIME2(7)     NULL,
    [RefreshToken]    NVARCHAR(500)    NULL,
    [RefreshTokenExp] DATETIME2(7)     NULL,
    [CreatedAt]       DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]       DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Users_Username] UNIQUE ([Username]),
    CONSTRAINT [UQ_Users_Email]    UNIQUE ([Email]),
    CONSTRAINT [FK_Users_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id])
);

-- ============================================================
-- 2. RESOURCES (SQL execution log — ErrorMessage per your spec)
-- ============================================================
IF OBJECT_ID('dbo.Resources','U') IS NULL
CREATE TABLE [dbo].[Resources](
    [Id]           INT IDENTITY(1,1) NOT NULL,
    [FileName]     NVARCHAR(500)    NOT NULL,
    [FileContent]  NVARCHAR(MAX)    NULL,
    [ExecutedAt]   DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [IsSuccess]    BIT              NOT NULL DEFAULT 0,
    [ErrorMessage] NVARCHAR(MAX)    NULL,
    [CreatedAt]    DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]    DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Resources] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Resources_FileName] UNIQUE ([FileName])
);

-- ============================================================
-- 3. PORTFOLIO CONTENT TABLES
-- ============================================================
IF OBJECT_ID('dbo.Profile','U') IS NULL
CREATE TABLE [dbo].[Profile](
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [FullName]        NVARCHAR(200)    NOT NULL,
    [Title]           NVARCHAR(300)    NOT NULL,
    [TaglineLine1]    NVARCHAR(300)    NULL,
    [TaglineLine2]    NVARCHAR(300)    NULL,
    [TaglineLine3]    NVARCHAR(300)    NULL,
    [Bio]             NVARCHAR(MAX)    NULL,
    [ProfileImageUrl] NVARCHAR(500)    NULL,
    [ResumeFileUrl]   NVARCHAR(500)    NULL,
    [Email]           NVARCHAR(300)    NULL,
    [LinkedIn]        NVARCHAR(500)    NULL,
    [GitHub]          NVARCHAR(500)    NULL,
    [Twitter]         NVARCHAR(500)    NULL,
    [Location]        NVARCHAR(300)    NULL,
    [ShowreelUrl]     NVARCHAR(500)    NULL,
    [MetaDescription] NVARCHAR(160)    NULL,
    [MetaKeywords]    NVARCHAR(300)    NULL,
    [OgImageUrl]      NVARCHAR(500)    NULL,
    [GoogleAnalyticsId] NVARCHAR(100)  NULL,
    [SiteUrl]         NVARCHAR(300)    NULL,
    [CreatedAt]       DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]       DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Profile] PRIMARY KEY CLUSTERED ([Id] ASC)
);

IF OBJECT_ID('dbo.Projects','U') IS NULL
CREATE TABLE [dbo].[Projects](
    [Id]             INT IDENTITY(1,1) NOT NULL,
    [Title]          NVARCHAR(300)    NOT NULL,
    [Slug]           NVARCHAR(300)    NOT NULL,
    [Description]    NVARCHAR(MAX)    NULL,
    [ShortDesc]      NVARCHAR(500)    NULL,
    [Category]       NVARCHAR(200)    NULL,
    [ThumbnailUrl]   NVARCHAR(500)    NULL,
    [ImageUrls]      NVARCHAR(MAX)    NULL,
    [LiveUrl]        NVARCHAR(500)    NULL,
    [GitHubUrl]      NVARCHAR(500)    NULL,
    [Technologies]   NVARCHAR(MAX)    NULL,
    [MockupClass]    NVARCHAR(100)    NULL DEFAULT 'mockup-dark',
    [CardRotation]   NVARCHAR(20)     NULL DEFAULT '-2deg',
    [SortOrder]      INT              NOT NULL DEFAULT 0,
    [IsFeatured]     BIT              NOT NULL DEFAULT 0,
    [IsPublished]    BIT              NOT NULL DEFAULT 1,
    [CompletedAt]    DATE             NULL,
    [CreatedAt]      DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]      DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Projects] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Projects_Slug] UNIQUE ([Slug])
);

IF OBJECT_ID('dbo.ProjectImages','U') IS NULL
CREATE TABLE [dbo].[ProjectImages](
    [Id]          INT IDENTITY(1,1) NOT NULL,
    [ProjectId]   INT               NOT NULL,
    [ImageUrl]    NVARCHAR(500)     NOT NULL,
    [AltText]     NVARCHAR(300)     NULL,
    [SortOrder]   INT               NOT NULL DEFAULT 0,
    [CreatedAt]   DATETIME2(7)      NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_ProjectImages] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ProjectImages_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Projects]([Id]) ON DELETE CASCADE
);

IF OBJECT_ID('dbo.Skills','U') IS NULL
CREATE TABLE [dbo].[Skills](
    [Id]          INT IDENTITY(1,1) NOT NULL,
    [Name]        NVARCHAR(200)    NOT NULL,
    [Category]    NVARCHAR(200)    NOT NULL,
    [Proficiency] INT              NOT NULL DEFAULT 0,
    [IconClass]   NVARCHAR(200)    NULL,
    [IsTag]       BIT              NOT NULL DEFAULT 0,
    [SortOrder]   INT              NOT NULL DEFAULT 0,
    [CreatedAt]   DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]   DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Skills] PRIMARY KEY CLUSTERED ([Id] ASC)
);

IF OBJECT_ID('dbo.Experiences','U') IS NULL
CREATE TABLE [dbo].[Experiences](
    [Id]           INT IDENTITY(1,1) NOT NULL,
    [Company]      NVARCHAR(300)    NOT NULL,
    [Role]         NVARCHAR(300)    NOT NULL,
    [Description]  NVARCHAR(MAX)    NULL,
    [StartDate]    DATE             NOT NULL,
    [EndDate]      DATE             NULL,
    [Location]     NVARCHAR(300)    NULL,
    [CompanyUrl]   NVARCHAR(500)    NULL,
    [LogoUrl]      NVARCHAR(500)    NULL,
    [SortOrder]    INT              NOT NULL DEFAULT 0,
    [CreatedAt]    DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]    DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Experiences] PRIMARY KEY CLUSTERED ([Id] ASC)
);

IF OBJECT_ID('dbo.Testimonials','U') IS NULL
CREATE TABLE [dbo].[Testimonials](
    [Id]           INT IDENTITY(1,1) NOT NULL,
    [ClientName]   NVARCHAR(300)    NOT NULL,
    [ClientTitle]  NVARCHAR(300)    NULL,
    [ClientAvatar] NVARCHAR(500)    NULL,
    [Content]      NVARCHAR(MAX)    NOT NULL,
    [Rating]       INT              NOT NULL DEFAULT 5,
    [ProjectId]    INT              NULL,
    [IsPublished]  BIT              NOT NULL DEFAULT 1,
    [SortOrder]    INT              NOT NULL DEFAULT 0,
    [CreatedAt]    DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]    DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Testimonials] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Testimonials_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Projects]([Id]) ON DELETE SET NULL
);

IF OBJECT_ID('dbo.ContactMessages','U') IS NULL
CREATE TABLE [dbo].[ContactMessages](
    [Id]        INT IDENTITY(1,1) NOT NULL,
    [Name]      NVARCHAR(300)    NOT NULL,
    [Email]     NVARCHAR(300)    NOT NULL,
    [Subject]   NVARCHAR(500)    NULL,
    [Message]   NVARCHAR(MAX)    NOT NULL,
    [IsRead]    BIT              NOT NULL DEFAULT 0,
    [RepliedAt] DATETIME2(7)     NULL,
    [CreatedAt] DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_ContactMessages] PRIMARY KEY CLUSTERED ([Id] ASC)
);

IF OBJECT_ID('dbo.SiteSettings','U') IS NULL
CREATE TABLE [dbo].[SiteSettings](
    [Id]        INT IDENTITY(1,1) NOT NULL,
    [Key]       NVARCHAR(200)    NOT NULL,
    [Value]     NVARCHAR(MAX)    NULL,
    [UpdatedAt] DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_SiteSettings] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_SiteSettings_Key] UNIQUE ([Key])
);
GO

-- ============================================================
-- 4. STORED PROCEDURES
-- ============================================================

-- Resource execution with ErrorMessage logging
IF OBJECT_ID('dbo.usp_ExecuteResource','P') IS NOT NULL DROP PROCEDURE dbo.usp_ExecuteResource;
GO
CREATE PROCEDURE dbo.usp_ExecuteResource
    @FileName    NVARCHAR(500),
    @FileContent NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Err NVARCHAR(MAX)=NULL, @Ok BIT=1;
    BEGIN TRY
        EXEC sp_executesql @FileContent;
    END TRY
    BEGIN CATCH
        SET @Ok=0;
        SET @Err=CONCAT('LINE:',ERROR_LINE(),' | MSG:',ERROR_MESSAGE(),' | PROC:',ISNULL(ERROR_PROCEDURE(),'N/A'),' | STATE:',ERROR_STATE());
    END CATCH

    IF EXISTS(SELECT 1 FROM Resources WHERE FileName=@FileName)
        UPDATE Resources SET FileContent=@FileContent,ExecutedAt=SYSUTCDATETIME(),IsSuccess=@Ok,ErrorMessage=@Err,UpdatedAt=SYSUTCDATETIME()
        WHERE FileName=@FileName;
    ELSE
        INSERT INTO Resources(FileName,FileContent,IsSuccess,ErrorMessage) VALUES(@FileName,@FileContent,@Ok,@Err);
END
GO

-- User authentication
IF OBJECT_ID('dbo.usp_AuthenticateUser','P') IS NOT NULL DROP PROCEDURE dbo.usp_AuthenticateUser;
GO
CREATE PROCEDURE dbo.usp_AuthenticateUser
    @Username  NVARCHAR(100),
    @Password  NVARCHAR(500)
AS
BEGIN
    SELECT u.Id,u.Username,u.Email,u.FullName,u.AvatarUrl,u.RoleId,r.Name AS RoleName,u.IsActive
    FROM Users u JOIN Roles r ON u.RoleId=r.Id
    WHERE u.Username=@Username AND u.PasswordHash=@Password AND u.IsActive=1;
END
GO

PRINT '✓ PortfolioDB — all tables & procedures created.';
GO
