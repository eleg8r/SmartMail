-- Campaign Stored Procedures

USE SmartMailDb;
GO

-- Get Campaign by Id
CREATE OR ALTER PROCEDURE sp_Campaign_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM EmailCampaigns WHERE Id = @Id;

    -- Get recipients
    SELECT * FROM CampaignRecipients WHERE CampaignId = @Id;

    -- Get batch schedules
    SELECT * FROM BatchSchedules WHERE CampaignId = @Id ORDER BY BatchNumber;

    -- Get variants
    SELECT * FROM CampaignVariants WHERE CampaignId = @Id;

    -- Get drip steps
    SELECT * FROM DripSteps WHERE CampaignId = @Id ORDER BY StepNumber;
END
GO

-- Get Campaigns by TenantId with pagination
CREATE OR ALTER PROCEDURE sp_Campaign_GetByTenantId
    @TenantId UNIQUEIDENTIFIER,
    @Skip INT = 0,
    @Take INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM EmailCampaigns
    WHERE TenantId = @TenantId
    ORDER BY CreatedAt DESC
    OFFSET @Skip ROWS
    FETCH NEXT @Take ROWS ONLY;
END
GO

-- Add Campaign
CREATE OR ALTER PROCEDURE sp_Campaign_Add
    @Id UNIQUEIDENTIFIER OUTPUT,
    @TenantId UNIQUEIDENTIFIER,
    @Name NVARCHAR(200),
    @Description NVARCHAR(1000) = NULL,
    @Type NVARCHAR(50),
    @Status NVARCHAR(50) = 'Draft',
    @FromAddress NVARCHAR(256),
    @FromDisplayName NVARCHAR(256) = NULL,
    @Subject NVARCHAR(500),
    @HtmlTemplate NVARCHAR(MAX),
    @TextTemplate NVARCHAR(MAX) = NULL,
    @ScheduledStartDate DATETIME2 = NULL,
    @ScheduledEndDate DATETIME2 = NULL,
    @TimeZone NVARCHAR(100) = NULL,
    @BatchSize INT = NULL,
    @MaxEmailsPerHour INT = NULL,
    @MaxEmailsPerDay INT = NULL,
    @IsAbTest BIT = 0,
    @IsDripCampaign BIT = 0,
    @IncludeUnsubscribeLink BIT = 1,
    @UnsubscribeUrl NVARCHAR(500) = NULL,
    @EnableOpenTracking BIT = 1,
    @EnableClickTracking BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF @Id IS NULL
        SET @Id = NEWID();

    INSERT INTO EmailCampaigns (
        Id, TenantId, Name, Description, Type, Status, FromAddress, FromDisplayName,
        Subject, HtmlTemplate, TextTemplate, ScheduledStartDate, ScheduledEndDate,
        TimeZone, BatchSize, MaxEmailsPerHour, MaxEmailsPerDay, IsAbTest,
        IsDripCampaign, IncludeUnsubscribeLink, UnsubscribeUrl,
        EnableOpenTracking, EnableClickTracking, CreatedAt
    )
    VALUES (
        @Id, @TenantId, @Name, @Description, @Type, @Status, @FromAddress, @FromDisplayName,
        @Subject, @HtmlTemplate, @TextTemplate, @ScheduledStartDate, @ScheduledEndDate,
        @TimeZone, @BatchSize, @MaxEmailsPerHour, @MaxEmailsPerDay, @IsAbTest,
        @IsDripCampaign, @IncludeUnsubscribeLink, @UnsubscribeUrl,
        @EnableOpenTracking, @EnableClickTracking, GETUTCDATE()
    );

    SELECT @Id AS Id;
END
GO

-- Update Campaign
CREATE OR ALTER PROCEDURE sp_Campaign_Update
    @Id UNIQUEIDENTIFIER,
    @Status NVARCHAR(50) = NULL,
    @TotalRecipients INT = NULL,
    @EmailsSent INT = NULL,
    @EmailsDelivered INT = NULL,
    @EmailsOpened INT = NULL,
    @EmailsClicked INT = NULL,
    @EmailsBounced INT = NULL,
    @EmailsFailed INT = NULL,
    @Unsubscribes INT = NULL,
    @SpamComplaints INT = NULL,
    @StartedAt DATETIME2 = NULL,
    @CompletedAt DATETIME2 = NULL,
    @PausedAt DATETIME2 = NULL,
    @CancelledAt DATETIME2 = NULL,
    @WinningVariantId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE EmailCampaigns
    SET
        Status = COALESCE(@Status, Status),
        TotalRecipients = COALESCE(@TotalRecipients, TotalRecipients),
        EmailsSent = COALESCE(@EmailsSent, EmailsSent),
        EmailsDelivered = COALESCE(@EmailsDelivered, EmailsDelivered),
        EmailsOpened = COALESCE(@EmailsOpened, EmailsOpened),
        EmailsClicked = COALESCE(@EmailsClicked, EmailsClicked),
        EmailsBounced = COALESCE(@EmailsBounced, EmailsBounced),
        EmailsFailed = COALESCE(@EmailsFailed, EmailsFailed),
        Unsubscribes = COALESCE(@Unsubscribes, Unsubscribes),
        SpamComplaints = COALESCE(@SpamComplaints, SpamComplaints),
        StartedAt = COALESCE(@StartedAt, StartedAt),
        CompletedAt = COALESCE(@CompletedAt, CompletedAt),
        PausedAt = @PausedAt, -- Allow NULL to clear
        CancelledAt = COALESCE(@CancelledAt, CancelledAt),
        WinningVariantId = COALESCE(@WinningVariantId, WinningVariantId),
        UpdatedAt = GETUTCDATE()
    WHERE Id = @Id;
END
GO

-- Update Campaign Statistics (increment counters)
CREATE OR ALTER PROCEDURE sp_Campaign_UpdateStatistics
    @Id UNIQUEIDENTIFIER,
    @IncrementSent BIT = 0,
    @IncrementDelivered BIT = 0,
    @IncrementOpened BIT = 0,
    @IncrementClicked BIT = 0,
    @IncrementBounced BIT = 0,
    @IncrementFailed BIT = 0,
    @IncrementUnsubscribes BIT = 0,
    @IncrementSpamComplaints BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE EmailCampaigns
    SET
        EmailsSent = EmailsSent + CASE WHEN @IncrementSent = 1 THEN 1 ELSE 0 END,
        EmailsDelivered = EmailsDelivered + CASE WHEN @IncrementDelivered = 1 THEN 1 ELSE 0 END,
        EmailsOpened = EmailsOpened + CASE WHEN @IncrementOpened = 1 THEN 1 ELSE 0 END,
        EmailsClicked = EmailsClicked + CASE WHEN @IncrementClicked = 1 THEN 1 ELSE 0 END,
        EmailsBounced = EmailsBounced + CASE WHEN @IncrementBounced = 1 THEN 1 ELSE 0 END,
        EmailsFailed = EmailsFailed + CASE WHEN @IncrementFailed = 1 THEN 1 ELSE 0 END,
        Unsubscribes = Unsubscribes + CASE WHEN @IncrementUnsubscribes = 1 THEN 1 ELSE 0 END,
        SpamComplaints = SpamComplaints + CASE WHEN @IncrementSpamComplaints = 1 THEN 1 ELSE 0 END,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @Id;
END
GO

-- Add Campaign Recipient
CREATE OR ALTER PROCEDURE sp_CampaignRecipient_Add
    @Id UNIQUEIDENTIFIER OUTPUT,
    @CampaignId UNIQUEIDENTIFIER,
    @EmailAddress NVARCHAR(256),
    @EmailDisplayName NVARCHAR(256) = NULL,
    @PersonalizationData NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Id IS NULL
        SET @Id = NEWID();

    INSERT INTO CampaignRecipients (
        Id, CampaignId, EmailAddress, EmailDisplayName, PersonalizationData, CreatedAt
    )
    VALUES (
        @Id, @CampaignId, @EmailAddress, @EmailDisplayName, @PersonalizationData, GETUTCDATE()
    );

    SELECT @Id AS Id;
END
GO

-- Update Campaign Recipient
CREATE OR ALTER PROCEDURE sp_CampaignRecipient_Update
    @Id UNIQUEIDENTIFIER,
    @EmailId UNIQUEIDENTIFIER = NULL,
    @Sent BIT = NULL,
    @SentAt DATETIME2 = NULL,
    @VariantId UNIQUEIDENTIFIER = NULL,
    @DripStepIndex INT = NULL,
    @NextScheduledDate DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE CampaignRecipients
    SET
        EmailId = COALESCE(@EmailId, EmailId),
        Sent = COALESCE(@Sent, Sent),
        SentAt = COALESCE(@SentAt, SentAt),
        VariantId = COALESCE(@VariantId, VariantId),
        DripStepIndex = COALESCE(@DripStepIndex, DripStepIndex),
        NextScheduledDate = @NextScheduledDate
    WHERE Id = @Id;
END
GO

-- Get Campaign Recipients
CREATE OR ALTER PROCEDURE sp_CampaignRecipient_GetByCampaignId
    @CampaignId UNIQUEIDENTIFIER,
    @SentOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @SentOnly = 1
        SELECT * FROM CampaignRecipients
        WHERE CampaignId = @CampaignId AND Sent = 1;
    ELSE
        SELECT * FROM CampaignRecipients
        WHERE CampaignId = @CampaignId;
END
GO

-- Add Batch Schedule
CREATE OR ALTER PROCEDURE sp_BatchSchedule_Add
    @Id UNIQUEIDENTIFIER OUTPUT,
    @CampaignId UNIQUEIDENTIFIER,
    @BatchNumber INT,
    @ScheduledTime DATETIME2,
    @MaxRecipients INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Id IS NULL
        SET @Id = NEWID();

    INSERT INTO BatchSchedules (
        Id, CampaignId, BatchNumber, ScheduledTime, MaxRecipients, CreatedAt
    )
    VALUES (
        @Id, @CampaignId, @BatchNumber, @ScheduledTime, @MaxRecipients, GETUTCDATE()
    );

    SELECT @Id AS Id;
END
GO

-- Update Batch Schedule
CREATE OR ALTER PROCEDURE sp_BatchSchedule_Update
    @Id UNIQUEIDENTIFIER,
    @Completed BIT = NULL,
    @CompletedAt DATETIME2 = NULL,
    @RecipientsSent INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE BatchSchedules
    SET
        Completed = COALESCE(@Completed, Completed),
        CompletedAt = COALESCE(@CompletedAt, CompletedAt),
        RecipientsSent = COALESCE(@RecipientsSent, RecipientsSent)
    WHERE Id = @Id;
END
GO

PRINT 'Campaign stored procedures created successfully!';
