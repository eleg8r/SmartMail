-- Email Stored Procedures

USE SmartMailDb;
GO

-- Get Email by Id
CREATE OR ALTER PROCEDURE sp_Email_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM Emails WHERE Id = @Id;

    -- Get attachments
    SELECT * FROM EmailAttachments WHERE EmailId = @Id;
END
GO

-- Get Email by TrackingId
CREATE OR ALTER PROCEDURE sp_Email_GetByTrackingId
    @TrackingId NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM Emails WHERE TrackingId = @TrackingId;
END
GO

-- Get Emails by TenantId with pagination
CREATE OR ALTER PROCEDURE sp_Email_GetByTenantId
    @TenantId UNIQUEIDENTIFIER,
    @Skip INT = 0,
    @Take INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM Emails
    WHERE TenantId = @TenantId
    ORDER BY CreatedAt DESC
    OFFSET @Skip ROWS
    FETCH NEXT @Take ROWS ONLY;
END
GO

-- Add Email
CREATE OR ALTER PROCEDURE sp_Email_Add
    @Id UNIQUEIDENTIFIER OUTPUT,
    @TenantId UNIQUEIDENTIFIER,
    @FromAddress NVARCHAR(256),
    @FromDisplayName NVARCHAR(256) = NULL,
    @ToAddress NVARCHAR(256),
    @ToDisplayName NVARCHAR(256) = NULL,
    @CcList NVARCHAR(MAX) = NULL,
    @BccList NVARCHAR(MAX) = NULL,
    @Subject NVARCHAR(500),
    @HtmlBody NVARCHAR(MAX),
    @TextBody NVARCHAR(MAX) = NULL,
    @Status NVARCHAR(50) = 'Queued',
    @CampaignId UNIQUEIDENTIFIER = NULL,
    @TrackingId NVARCHAR(100) = NULL,
    @ScheduledAt DATETIME2 = NULL,
    @Metadata NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Id IS NULL
        SET @Id = NEWID();

    INSERT INTO Emails (
        Id, TenantId, FromAddress, FromDisplayName, ToAddress, ToDisplayName,
        CcList, BccList, Subject, HtmlBody, TextBody, Status, CampaignId,
        TrackingId, ScheduledAt, Metadata, CreatedAt
    )
    VALUES (
        @Id, @TenantId, @FromAddress, @FromDisplayName, @ToAddress, @ToDisplayName,
        @CcList, @BccList, @Subject, @HtmlBody, @TextBody, @Status, @CampaignId,
        @TrackingId, @ScheduledAt, @Metadata, GETUTCDATE()
    );

    SELECT @Id AS Id;
END
GO

-- Update Email
CREATE OR ALTER PROCEDURE sp_Email_Update
    @Id UNIQUEIDENTIFIER,
    @Status NVARCHAR(50) = NULL,
    @OpenTracked BIT = NULL,
    @OpenedAt DATETIME2 = NULL,
    @OpenCount INT = NULL,
    @ClickTracked BIT = NULL,
    @FirstClickedAt DATETIME2 = NULL,
    @ClickCount INT = NULL,
    @SentAt DATETIME2 = NULL,
    @DeliveredAt DATETIME2 = NULL,
    @BouncedAt DATETIME2 = NULL,
    @BounceType NVARCHAR(50) = NULL,
    @BounceReason NVARCHAR(1000) = NULL,
    @RetryCount INT = NULL,
    @ErrorMessage NVARCHAR(2000) = NULL,
    @ProviderUsed NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Emails
    SET
        Status = COALESCE(@Status, Status),
        OpenTracked = COALESCE(@OpenTracked, OpenTracked),
        OpenedAt = COALESCE(@OpenedAt, OpenedAt),
        OpenCount = COALESCE(@OpenCount, OpenCount),
        ClickTracked = COALESCE(@ClickTracked, ClickTracked),
        FirstClickedAt = COALESCE(@FirstClickedAt, FirstClickedAt),
        ClickCount = COALESCE(@ClickCount, ClickCount),
        SentAt = COALESCE(@SentAt, SentAt),
        DeliveredAt = COALESCE(@DeliveredAt, DeliveredAt),
        BouncedAt = COALESCE(@BouncedAt, BouncedAt),
        BounceType = COALESCE(@BounceType, BounceType),
        BounceReason = COALESCE(@BounceReason, BounceReason),
        RetryCount = COALESCE(@RetryCount, RetryCount),
        ErrorMessage = COALESCE(@ErrorMessage, ErrorMessage),
        ProviderUsed = COALESCE(@ProviderUsed, ProviderUsed),
        UpdatedAt = GETUTCDATE()
    WHERE Id = @Id;
END
GO

-- Add Email Attachment
CREATE OR ALTER PROCEDURE sp_EmailAttachment_Add
    @Id UNIQUEIDENTIFIER OUTPUT,
    @EmailId UNIQUEIDENTIFIER,
    @FileName NVARCHAR(500),
    @ContentType NVARCHAR(200),
    @SizeInBytes BIGINT,
    @Content VARBINARY(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Id IS NULL
        SET @Id = NEWID();

    INSERT INTO EmailAttachments (Id, EmailId, FileName, ContentType, SizeInBytes, Content, CreatedAt)
    VALUES (@Id, @EmailId, @FileName, @ContentType, @SizeInBytes, @Content, GETUTCDATE());

    SELECT @Id AS Id;
END
GO

PRINT 'Email stored procedures created successfully!';
