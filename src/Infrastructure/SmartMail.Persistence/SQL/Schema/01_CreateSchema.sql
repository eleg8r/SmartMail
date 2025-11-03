-- SmartMail Database Schema
-- SQL Server 2019+

USE master;
GO

-- Create database if not exists
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SmartMailDb')
BEGIN
    CREATE DATABASE SmartMailDb;
END
GO

USE SmartMailDb;
GO

-- Tenants Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tenants')
BEGIN
    CREATE TABLE Tenants (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        TenantId UNIQUEIDENTIFIER NOT NULL UNIQUE,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        DefaultEmailProvider NVARCHAR(50) NOT NULL DEFAULT 'Smtp',
        MaxAttachmentSizeInMb INT NOT NULL DEFAULT 25,
        MaxEmailsPerHour INT NOT NULL DEFAULT 1000,
        MaxEmailsPerDay INT NOT NULL DEFAULT 10000,
        EnableOpenTracking BIT NOT NULL DEFAULT 1,
        EnableClickTracking BIT NOT NULL DEFAULT 1,
        ApiKey NVARCHAR(256) NOT NULL,
        ApiKeyCreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ApiKeyExpiresAt DATETIME2 NULL,
        ContactEmail NVARCHAR(256) NULL,
        ContactName NVARCHAR(256) NULL,
        SubscriptionStartDate DATETIME2 NULL,
        SubscriptionEndDate DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CreatedBy NVARCHAR(256) NULL,
        UpdatedBy NVARCHAR(256) NULL,
        INDEX IX_Tenants_TenantId (TenantId),
        INDEX IX_Tenants_ApiKey (ApiKey)
    );
END
GO

-- Emails Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Emails')
BEGIN
    CREATE TABLE Emails (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        TenantId UNIQUEIDENTIFIER NOT NULL,
        FromAddress NVARCHAR(256) NOT NULL,
        FromDisplayName NVARCHAR(256) NULL,
        ToAddress NVARCHAR(256) NOT NULL,
        ToDisplayName NVARCHAR(256) NULL,
        CcList NVARCHAR(MAX) NULL, -- JSON array
        BccList NVARCHAR(MAX) NULL, -- JSON array
        Subject NVARCHAR(500) NOT NULL,
        HtmlBody NVARCHAR(MAX) NOT NULL,
        TextBody NVARCHAR(MAX) NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Queued',
        CampaignId UNIQUEIDENTIFIER NULL,
        TrackingId NVARCHAR(100) NULL UNIQUE,
        OpenTracked BIT NOT NULL DEFAULT 0,
        OpenedAt DATETIME2 NULL,
        OpenCount INT NOT NULL DEFAULT 0,
        ClickTracked BIT NOT NULL DEFAULT 0,
        FirstClickedAt DATETIME2 NULL,
        ClickCount INT NOT NULL DEFAULT 0,
        ScheduledAt DATETIME2 NULL,
        SentAt DATETIME2 NULL,
        DeliveredAt DATETIME2 NULL,
        BouncedAt DATETIME2 NULL,
        BounceType NVARCHAR(50) NULL,
        BounceReason NVARCHAR(1000) NULL,
        Metadata NVARCHAR(MAX) NULL, -- JSON object
        RetryCount INT NOT NULL DEFAULT 0,
        ErrorMessage NVARCHAR(2000) NULL,
        ProviderUsed NVARCHAR(50) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        INDEX IX_Emails_TenantId (TenantId),
        INDEX IX_Emails_CampaignId (CampaignId),
        INDEX IX_Emails_Status (Status),
        INDEX IX_Emails_TrackingId (TrackingId),
        INDEX IX_Emails_ScheduledAt (ScheduledAt),
        INDEX IX_Emails_CreatedAt (CreatedAt)
    );
END
GO

-- EmailAttachments Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmailAttachments')
BEGIN
    CREATE TABLE EmailAttachments (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        EmailId UNIQUEIDENTIFIER NOT NULL,
        FileName NVARCHAR(500) NOT NULL,
        ContentType NVARCHAR(200) NOT NULL,
        SizeInBytes BIGINT NOT NULL,
        Content VARBINARY(MAX) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        FOREIGN KEY (EmailId) REFERENCES Emails(Id) ON DELETE CASCADE,
        INDEX IX_EmailAttachments_EmailId (EmailId)
    );
END
GO

-- EmailCampaigns Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmailCampaigns')
BEGIN
    CREATE TABLE EmailCampaigns (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        TenantId UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        Type NVARCHAR(50) NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Draft',
        FromAddress NVARCHAR(256) NOT NULL,
        FromDisplayName NVARCHAR(256) NULL,
        Subject NVARCHAR(500) NOT NULL,
        HtmlTemplate NVARCHAR(MAX) NOT NULL,
        TextTemplate NVARCHAR(MAX) NULL,
        RecipientListIds NVARCHAR(MAX) NULL, -- JSON array
        ScheduledStartDate DATETIME2 NULL,
        ScheduledEndDate DATETIME2 NULL,
        TimeZone NVARCHAR(100) NULL,
        BatchSize INT NULL,
        MaxEmailsPerHour INT NULL,
        MaxEmailsPerDay INT NULL,
        IsAbTest BIT NOT NULL DEFAULT 0,
        AbTestSampleSize INT NULL,
        AbTestEndDate DATETIME2 NULL,
        WinningVariantId UNIQUEIDENTIFIER NULL,
        IsDripCampaign BIT NOT NULL DEFAULT 0,
        IncludeUnsubscribeLink BIT NOT NULL DEFAULT 1,
        UnsubscribeUrl NVARCHAR(500) NULL,
        RequireDoubleOptIn BIT NOT NULL DEFAULT 0,
        EnableOpenTracking BIT NOT NULL DEFAULT 1,
        EnableClickTracking BIT NOT NULL DEFAULT 1,
        TotalRecipients INT NOT NULL DEFAULT 0,
        EmailsSent INT NOT NULL DEFAULT 0,
        EmailsDelivered INT NOT NULL DEFAULT 0,
        EmailsOpened INT NOT NULL DEFAULT 0,
        EmailsClicked INT NOT NULL DEFAULT 0,
        EmailsBounced INT NOT NULL DEFAULT 0,
        EmailsFailed INT NOT NULL DEFAULT 0,
        Unsubscribes INT NOT NULL DEFAULT 0,
        SpamComplaints INT NOT NULL DEFAULT 0,
        Metadata NVARCHAR(MAX) NULL, -- JSON object
        StartedAt DATETIME2 NULL,
        CompletedAt DATETIME2 NULL,
        PausedAt DATETIME2 NULL,
        CancelledAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        INDEX IX_EmailCampaigns_TenantId (TenantId),
        INDEX IX_EmailCampaigns_Status (Status),
        INDEX IX_EmailCampaigns_ScheduledStartDate (ScheduledStartDate),
        INDEX IX_EmailCampaigns_CreatedAt (CreatedAt)
    );
END
GO

-- CampaignRecipients Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CampaignRecipients')
BEGIN
    CREATE TABLE CampaignRecipients (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        CampaignId UNIQUEIDENTIFIER NOT NULL,
        EmailAddress NVARCHAR(256) NOT NULL,
        EmailDisplayName NVARCHAR(256) NULL,
        PersonalizationData NVARCHAR(MAX) NULL, -- JSON object
        EmailId UNIQUEIDENTIFIER NULL,
        Sent BIT NOT NULL DEFAULT 0,
        SentAt DATETIME2 NULL,
        VariantId UNIQUEIDENTIFIER NULL,
        DripStepIndex INT NULL,
        NextScheduledDate DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        FOREIGN KEY (CampaignId) REFERENCES EmailCampaigns(Id) ON DELETE CASCADE,
        INDEX IX_CampaignRecipients_CampaignId (CampaignId),
        INDEX IX_CampaignRecipients_Sent (Sent)
    );
END
GO

-- BatchSchedules Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BatchSchedules')
BEGIN
    CREATE TABLE BatchSchedules (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        CampaignId UNIQUEIDENTIFIER NOT NULL,
        BatchNumber INT NOT NULL,
        ScheduledTime DATETIME2 NOT NULL,
        MaxRecipients INT NOT NULL,
        Completed BIT NOT NULL DEFAULT 0,
        CompletedAt DATETIME2 NULL,
        RecipientsSent INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        FOREIGN KEY (CampaignId) REFERENCES EmailCampaigns(Id) ON DELETE CASCADE,
        INDEX IX_BatchSchedules_CampaignId (CampaignId),
        INDEX IX_BatchSchedules_ScheduledTime (ScheduledTime)
    );
END
GO

-- CampaignVariants Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CampaignVariants')
BEGIN
    CREATE TABLE CampaignVariants (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        CampaignId UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Subject NVARCHAR(500) NOT NULL,
        HtmlContent NVARCHAR(MAX) NOT NULL,
        TextContent NVARCHAR(MAX) NULL,
        Weight INT NOT NULL DEFAULT 50,
        SentCount INT NOT NULL DEFAULT 0,
        OpenedCount INT NOT NULL DEFAULT 0,
        ClickedCount INT NOT NULL DEFAULT 0,
        BouncedCount INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        FOREIGN KEY (CampaignId) REFERENCES EmailCampaigns(Id) ON DELETE CASCADE,
        INDEX IX_CampaignVariants_CampaignId (CampaignId)
    );
END
GO

-- DripSteps Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DripSteps')
BEGIN
    CREATE TABLE DripSteps (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        CampaignId UNIQUEIDENTIFIER NOT NULL,
        StepNumber INT NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Subject NVARCHAR(500) NOT NULL,
        HtmlContent NVARCHAR(MAX) NOT NULL,
        TextContent NVARCHAR(MAX) NULL,
        DelayInDays INT NOT NULL DEFAULT 0,
        DelayInHours INT NOT NULL DEFAULT 0,
        DelayInMinutes INT NOT NULL DEFAULT 0,
        TriggerCondition NVARCHAR(MAX) NULL,
        SentCount INT NOT NULL DEFAULT 0,
        OpenedCount INT NOT NULL DEFAULT 0,
        ClickedCount INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        FOREIGN KEY (CampaignId) REFERENCES EmailCampaigns(Id) ON DELETE CASCADE,
        INDEX IX_DripSteps_CampaignId (CampaignId)
    );
END
GO

-- EmailClicks Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmailClicks')
BEGIN
    CREATE TABLE EmailClicks (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        TenantId UNIQUEIDENTIFIER NOT NULL,
        EmailId UNIQUEIDENTIFIER NOT NULL,
        CampaignId UNIQUEIDENTIFIER NULL,
        RecipientEmail NVARCHAR(256) NOT NULL,
        OriginalUrl NVARCHAR(2000) NOT NULL,
        TrackedUrl NVARCHAR(2000) NOT NULL,
        ClickedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        IpAddress NVARCHAR(50) NULL,
        UserAgent NVARCHAR(500) NULL,
        Country NVARCHAR(100) NULL,
        City NVARCHAR(100) NULL,
        Device NVARCHAR(100) NULL,
        INDEX IX_EmailClicks_EmailId (EmailId),
        INDEX IX_EmailClicks_CampaignId (CampaignId),
        INDEX IX_EmailClicks_ClickedAt (ClickedAt)
    );
END
GO

-- UnsubscribeRequests Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UnsubscribeRequests')
BEGIN
    CREATE TABLE UnsubscribeRequests (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        TenantId UNIQUEIDENTIFIER NOT NULL,
        EmailAddress NVARCHAR(256) NOT NULL,
        CampaignId UNIQUEIDENTIFIER NULL,
        EmailId UNIQUEIDENTIFIER NULL,
        UnsubscribedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        Reason NVARCHAR(1000) NULL,
        IpAddress NVARCHAR(50) NULL,
        UserAgent NVARCHAR(500) NULL,
        GlobalUnsubscribe BIT NOT NULL DEFAULT 0,
        INDEX IX_UnsubscribeRequests_TenantId (TenantId),
        INDEX IX_UnsubscribeRequests_EmailAddress (EmailAddress),
        INDEX IX_UnsubscribeRequests_UnsubscribedAt (UnsubscribedAt)
    );
END
GO

PRINT 'Database schema created successfully!';
