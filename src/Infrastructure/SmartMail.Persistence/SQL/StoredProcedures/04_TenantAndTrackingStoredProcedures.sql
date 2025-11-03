-- Tenant and Tracking Stored Procedures

USE SmartMailDb;
GO

-- ============ TENANT PROCEDURES ============

-- Get Tenant by Id
CREATE OR ALTER PROCEDURE sp_Tenant_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM Tenants WHERE Id = @Id;
END
GO

-- Get Tenant by TenantId
CREATE OR ALTER PROCEDURE sp_Tenant_GetByTenantId
    @TenantId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM Tenants WHERE TenantId = @TenantId;
END
GO

-- Get Tenant by API Key
CREATE OR ALTER PROCEDURE sp_Tenant_GetByApiKey
    @ApiKey NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM Tenants WHERE ApiKey = @ApiKey AND IsActive = 1;
END
GO

-- Add Tenant
CREATE OR ALTER PROCEDURE sp_Tenant_Add
    @Id UNIQUEIDENTIFIER OUTPUT,
    @TenantId UNIQUEIDENTIFIER,
    @Name NVARCHAR(200),
    @Description NVARCHAR(1000) = NULL,
    @IsActive BIT = 1,
    @DefaultEmailProvider NVARCHAR(50) = 'Smtp',
    @MaxAttachmentSizeInMb INT = 25,
    @MaxEmailsPerHour INT = 1000,
    @MaxEmailsPerDay INT = 10000,
    @EnableOpenTracking BIT = 1,
    @EnableClickTracking BIT = 1,
    @ApiKey NVARCHAR(256),
    @ContactEmail NVARCHAR(256) = NULL,
    @ContactName NVARCHAR(256) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Id IS NULL
        SET @Id = NEWID();

    INSERT INTO Tenants (
        Id, TenantId, Name, Description, IsActive, DefaultEmailProvider,
        MaxAttachmentSizeInMb, MaxEmailsPerHour, MaxEmailsPerDay,
        EnableOpenTracking, EnableClickTracking, ApiKey,
        ContactEmail, ContactName, ApiKeyCreatedAt, CreatedAt
    )
    VALUES (
        @Id, @TenantId, @Name, @Description, @IsActive, @DefaultEmailProvider,
        @MaxAttachmentSizeInMb, @MaxEmailsPerHour, @MaxEmailsPerDay,
        @EnableOpenTracking, @EnableClickTracking, @ApiKey,
        @ContactEmail, @ContactName, GETUTCDATE(), GETUTCDATE()
    );

    SELECT @Id AS Id;
END
GO

-- Update Tenant
CREATE OR ALTER PROCEDURE sp_Tenant_Update
    @Id UNIQUEIDENTIFIER,
    @Name NVARCHAR(200) = NULL,
    @IsActive BIT = NULL,
    @DefaultEmailProvider NVARCHAR(50) = NULL,
    @MaxAttachmentSizeInMb INT = NULL,
    @MaxEmailsPerHour INT = NULL,
    @MaxEmailsPerDay INT = NULL,
    @EnableOpenTracking BIT = NULL,
    @EnableClickTracking BIT = NULL,
    @ApiKey NVARCHAR(256) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Tenants
    SET
        Name = COALESCE(@Name, Name),
        IsActive = COALESCE(@IsActive, IsActive),
        DefaultEmailProvider = COALESCE(@DefaultEmailProvider, DefaultEmailProvider),
        MaxAttachmentSizeInMb = COALESCE(@MaxAttachmentSizeInMb, MaxAttachmentSizeInMb),
        MaxEmailsPerHour = COALESCE(@MaxEmailsPerHour, MaxEmailsPerHour),
        MaxEmailsPerDay = COALESCE(@MaxEmailsPerDay, MaxEmailsPerDay),
        EnableOpenTracking = COALESCE(@EnableOpenTracking, EnableOpenTracking),
        EnableClickTracking = COALESCE(@EnableClickTracking, EnableClickTracking),
        ApiKey = COALESCE(@ApiKey, ApiKey),
        UpdatedAt = GETUTCDATE()
    WHERE Id = @Id;
END
GO

-- ============ EMAIL CLICK PROCEDURES ============

-- Add Email Click
CREATE OR ALTER PROCEDURE sp_EmailClick_Add
    @Id UNIQUEIDENTIFIER OUTPUT,
    @TenantId UNIQUEIDENTIFIER,
    @EmailId UNIQUEIDENTIFIER,
    @CampaignId UNIQUEIDENTIFIER = NULL,
    @RecipientEmail NVARCHAR(256),
    @OriginalUrl NVARCHAR(2000),
    @TrackedUrl NVARCHAR(2000),
    @IpAddress NVARCHAR(50) = NULL,
    @UserAgent NVARCHAR(500) = NULL,
    @Country NVARCHAR(100) = NULL,
    @City NVARCHAR(100) = NULL,
    @Device NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Id IS NULL
        SET @Id = NEWID();

    INSERT INTO EmailClicks (
        Id, TenantId, EmailId, CampaignId, RecipientEmail,
        OriginalUrl, TrackedUrl, IpAddress, UserAgent,
        Country, City, Device, ClickedAt
    )
    VALUES (
        @Id, @TenantId, @EmailId, @CampaignId, @RecipientEmail,
        @OriginalUrl, @TrackedUrl, @IpAddress, @UserAgent,
        @Country, @City, @Device, GETUTCDATE()
    );

    SELECT @Id AS Id;
END
GO

-- Get Email Clicks by Email Id
CREATE OR ALTER PROCEDURE sp_EmailClick_GetByEmailId
    @EmailId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM EmailClicks
    WHERE EmailId = @EmailId
    ORDER BY ClickedAt DESC;
END
GO

-- Get Email Clicks by Campaign Id
CREATE OR ALTER PROCEDURE sp_EmailClick_GetByCampaignId
    @CampaignId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM EmailClicks
    WHERE CampaignId = @CampaignId
    ORDER BY ClickedAt DESC;
END
GO

-- ============ UNSUBSCRIBE PROCEDURES ============

-- Add Unsubscribe Request
CREATE OR ALTER PROCEDURE sp_Unsubscribe_Add
    @Id UNIQUEIDENTIFIER OUTPUT,
    @TenantId UNIQUEIDENTIFIER,
    @EmailAddress NVARCHAR(256),
    @CampaignId UNIQUEIDENTIFIER = NULL,
    @EmailId UNIQUEIDENTIFIER = NULL,
    @Reason NVARCHAR(1000) = NULL,
    @IpAddress NVARCHAR(50) = NULL,
    @UserAgent NVARCHAR(500) = NULL,
    @GlobalUnsubscribe BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @Id IS NULL
        SET @Id = NEWID();

    INSERT INTO UnsubscribeRequests (
        Id, TenantId, EmailAddress, CampaignId, EmailId,
        Reason, IpAddress, UserAgent, GlobalUnsubscribe, UnsubscribedAt
    )
    VALUES (
        @Id, @TenantId, @EmailAddress, @CampaignId, @EmailId,
        @Reason, @IpAddress, @UserAgent, @GlobalUnsubscribe, GETUTCDATE()
    );

    SELECT @Id AS Id;
END
GO

-- Check if Email is Unsubscribed
CREATE OR ALTER PROCEDURE sp_Unsubscribe_IsUnsubscribed
    @TenantId UNIQUEIDENTIFIER,
    @EmailAddress NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(CASE WHEN EXISTS (
        SELECT 1 FROM UnsubscribeRequests
        WHERE TenantId = @TenantId
        AND EmailAddress = @EmailAddress
        AND GlobalUnsubscribe = 1
    ) THEN 1 ELSE 0 END AS BIT) AS IsUnsubscribed;
END
GO

-- Get Unsubscribes by Tenant
CREATE OR ALTER PROCEDURE sp_Unsubscribe_GetByTenantId
    @TenantId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM UnsubscribeRequests
    WHERE TenantId = @TenantId
    ORDER BY UnsubscribedAt DESC;
END
GO

PRINT 'Tenant and tracking stored procedures created successfully!';
