# SmartMail Architecture Documentation

## Overview

SmartMail is built using **Clean Architecture** and **Domain-Driven Design (DDD)** principles, ensuring maintainability, testability, and scalability.

## Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│                  PRESENTATION LAYER                      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │  gRPC API    │  │   REST API   │  │  Analytics   │  │
│  │  (Default)   │  │  (Optional)  │  │  Dashboard   │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
│         │                  │                  │          │
└─────────┼──────────────────┼──────────────────┼──────────┘
          │                  │                  │
          └──────────────────┴──────────────────┘
                             │
┌──────────────────────────────────────────────────────────┐
│                  APPLICATION LAYER                        │
│  ┌────────────────────────────────────────────────────┐  │
│  │         CQRS Commands & Queries (MediatR)          │  │
│  │  ┌──────────────┐         ┌──────────────┐        │  │
│  │  │   Commands   │         │   Queries    │        │  │
│  │  │ - SendEmail  │         │ - GetCamp    │        │  │
│  │  │ - CreateCamp │         │ - GetStats   │        │  │
│  │  └──────────────┘         └──────────────┘        │  │
│  └────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────┐  │
│  │              Interfaces & DTOs                     │  │
│  │  - IEmailService  - ISchedulerService              │  │
│  │  - ITrackingService  - ITemplateEngine             │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
                             │
┌──────────────────────────────────────────────────────────┐
│                 INFRASTRUCTURE LAYER                      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │
│  │Email Provid. │  │  Schedulers  │  │   Tracking   │   │
│  │- SMTP        │  │- Custom      │  │- Open/Click  │   │
│  │- SendGrid    │  │- Hangfire    │  │- Bounce      │   │
│  │- AWS SES     │  │- Quartz.NET  │  │- Spam        │   │
│  │- Mailgun     │  │- RabbitMQ    │  │              │   │
│  └──────────────┘  └──────────────┘  └──────────────┘   │
│  ┌───────────────────────────────────────────────────┐   │
│  │        Persistence (EF Core + SQL Server)         │   │
│  │  - DbContext  - Configurations  - Migrations      │   │
│  └───────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────┘
                             │
┌──────────────────────────────────────────────────────────┐
│                     DOMAIN LAYER                          │
│  ┌────────────────────────────────────────────────────┐  │
│  │                  Aggregates                        │  │
│  │  - Email (Root)                                    │  │
│  │  - EmailCampaign (Root)                            │  │
│  │  - Tenant (Root)                                   │  │
│  └────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────┐  │
│  │                Value Objects                       │  │
│  │  - EmailAddress  - EmailContent  - TenantId       │  │
│  └────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────┐  │
│  │                Domain Events                       │  │
│  │  - EmailSent  - CampaignStarted  - EmailOpened    │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
```

## Domain-Driven Design

### Aggregates

**Email Aggregate**
- Root: `Email`
- Entities: `EmailAttachment`
- Value Objects: `EmailAddress`, `EmailContent`
- Domain Events: `EmailSent`, `EmailOpened`, `EmailClicked`, `EmailBounced`

**Campaign Aggregate**
- Root: `EmailCampaign`
- Entities: `CampaignRecipient`, `BatchSchedule`, `CampaignVariant`, `DripStep`
- Domain Events: `CampaignStarted`, `CampaignCompleted`, `AbTestWinnerSelected`

**Tenant Aggregate**
- Root: `Tenant`
- Manages multi-tenancy isolation

### Bounded Contexts

1. **Email Context**: Email sending, tracking, delivery
2. **Campaign Context**: Campaign management, scheduling, analytics
3. **Tenant Context**: Multi-tenancy, configuration, limits

## Data Flow

### Sending a Single Email

```
Client Request (gRPC/REST)
    │
    ↓
API Layer (Validation)
    │
    ↓
SendEmailCommand (MediatR)
    │
    ↓
CommandHandler
    ├─→ Create Email Entity (Domain)
    ├─→ Save to Database (Persistence)
    ├─→ Apply Tracking (Infrastructure)
    └─→ Send via Email Provider (Infrastructure)
        ├─→ SMTP
        ├─→ SendGrid
        ├─→ AWS SES
        └─→ Mailgun
    │
    ↓
Response (Success/Failure)
```

### Running a Campaign

```
CreateCampaignCommand
    │
    ↓
Create Campaign Aggregate
    ├─→ Validate Template
    ├─→ Add Recipients
    ├─→ Configure Batches
    └─→ Save to DB
    │
    ↓
ScheduleCampaignCommand
    │
    ↓
Job Scheduler
    ├─→ Custom (In-Memory)
    ├─→ Hangfire (SQL Server)
    ├─→ Quartz.NET (Clustered)
    └─→ RabbitMQ (Distributed)
    │
    ↓
Process Batches (at scheduled times)
    ├─→ Get Recipients
    ├─→ Apply Personalization
    ├─→ Apply Tracking
    ├─→ Send Emails
    └─→ Update Campaign Stats
```

## Database Schema

### Core Tables

- **Emails**: Email records with status, tracking data
- **EmailAttachments**: Binary storage of attachments
- **EmailCampaigns**: Campaign definitions and stats
- **CampaignRecipients**: Individual recipients with personalization
- **BatchSchedules**: Scheduled batch execution times
- **CampaignVariants**: A/B test variants
- **DripSteps**: Drip campaign step definitions
- **EmailClicks**: Click tracking records
- **UnsubscribeRequests**: Unsubscribe records
- **Tenants**: Multi-tenant configuration

### Indexes

Key indexes for performance:
- `Emails`: TenantId, CampaignId, Status, TrackingId, ScheduledAt
- `EmailCampaigns`: TenantId, Status, ScheduledStartDate
- `CampaignRecipients`: CampaignId, Sent
- `EmailClicks`: EmailId, CampaignId

## Multi-Tenancy

SmartMail supports full multi-tenancy:

1. **Tenant Identification**: Via HTTP header `X-Tenant-ID`
2. **Data Isolation**: All queries filtered by TenantId
3. **Configuration**: Per-tenant limits and settings
4. **API Keys**: Per-tenant API key authentication

## Security

### Authentication Options

1. **API Key**: Simple header-based auth
   - Header: `X-API-Key: {tenant-api-key}`
   - Validated against Tenant table

2. **JWT**: Token-based auth
   - Standard OAuth 2.0 / JWT flow
   - Includes tenant claims

3. **Combined**: Both API Key and JWT supported

### Data Protection

- **Encryption at Rest**: SQL Server TDE (optional)
- **Encryption in Transit**: HTTPS/TLS
- **API Key Storage**: Hashed in database
- **Attachment Scanning**: Configurable (future)

## Scalability

### Horizontal Scaling

- **API Layer**: Stateless, can run multiple instances
- **Scheduler**: Quartz.NET clustering or RabbitMQ
- **Database**: SQL Server Always On / Read Replicas

### Performance Optimizations

- **Bulk Operations**: Batch email sending
- **Async Processing**: All I/O operations are async
- **Connection Pooling**: EF Core connection pooling
- **Caching**: In-memory caching for tenant config (future)

### Volume Handling

| Volume | Recommended Setup |
|--------|------------------|
| < 10k/day | Single server, SMTP, Custom scheduler |
| 10k-100k/day | 2-3 API servers, SendGrid, Hangfire |
| 100k-1M/day | Load balanced, AWS SES, Quartz.NET cluster |
| 1M+/day | Kubernetes, AWS SES, RabbitMQ, Read replicas |

## Monitoring & Observability

### Logging

- **Serilog**: Structured logging
- **Log Levels**: Debug, Info, Warning, Error
- **Sinks**: Console, File, Database (optional)

### Metrics

Key metrics to monitor:
- Email send rate
- Email delivery rate
- Bounce rate
- Open rate
- Click rate
- Queue depth
- API response time

### Health Checks

- Database connectivity
- Email provider connectivity
- Scheduler status
- Disk space (for logs)

## Deployment

### On-Premise Deployment

```
┌─────────────────┐
│  Load Balancer  │
└────────┬────────┘
         │
    ┌────┴────┐
    │         │
┌───▼──┐  ┌──▼───┐
│ API1 │  │ API2 │
└───┬──┘  └──┬───┘
    │        │
    └───┬────┘
        │
┌───────▼────────┐
│  SQL Server    │
└────────────────┘
```

### Docker Deployment

```yaml
version: '3.8'
services:
  api:
    image: smartmail-api:latest
    ports:
      - "5001:5001"
    environment:
      - ConnectionStrings__DefaultConnection=...

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - SA_PASSWORD=YourPassword
      - ACCEPT_EULA=Y
```

## Future Enhancements

1. **Redis Caching**: Cache tenant configuration
2. **Elasticsearch**: Advanced email search
3. **Webhooks**: Outbound webhook notifications
4. **Template Designer**: Visual template builder
5. **Email Warmup**: Gradual volume increase
6. **AI Optimization**: Send time optimization
7. **Advanced Segmentation**: ML-based segmentation

---

**Last Updated**: January 2025
