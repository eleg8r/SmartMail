# SmartMail - Production-Ready Email Marketing Service

SmartMail is a comprehensive email marketing service built with **Clean Architecture** and **Domain-Driven Design (DDD)** principles, designed to replicate and enhance ElasticMail functionality for .NET applications.

## 🚀 Features

### Core SmartMail Service
- ✉️ **Multi-Provider Email Sending**: Switchable between SMTP (default), SendGrid, AWS SES, and Mailgun
- 📊 **Advanced Tracking**: Open tracking, click tracking, bounce handling
- 🚫 **Unsubscribe Management**: Automated unsubscribe handling with compliance
- 📈 **Spam Complaint Handling**: Track and manage spam complaints
- 📎 **Attachment Support**: Configurable size limits with SQL Server storage
- 🏢 **Multi-Tenancy**: Full multi-tenant support with tenant isolation
- 🔐 **Switchable Authentication**: API Keys, OAuth 2.0/JWT, or combined

### EmailCampaign Service
- 📅 **Campaign Scheduling**: One-time, scheduled, or drip campaigns
- 📦 **Batch Processing**: Split campaigns into batches with specific time schedules
- 🧪 **A/B Testing**: Test multiple variants with automatic winner selection
- 🎯 **Personalization**: Token-based personalization ({{FirstName}}, etc.)
- 🔀 **Conditional Content**: Dynamic content based on recipient data
- 💧 **Drip Campaigns**: Automated email sequences with triggers
- 🌍 **Time Zone-Aware**: Send emails in recipients' time zones
- ⚡ **Rate Limiting**: Campaign-level and tenant-level throttling
- 📋 **CAN-SPAM & CASL Compliance**: Built-in compliance features

### Switchable Components
All major components are configurable via `appsettings.json`:

| Component | Options | Default |
|-----------|---------|---------|
| **Email Provider** | SMTP, SendGrid, AWS SES, Mailgun | SMTP |
| **Job Scheduler** | Custom, Hangfire, Quartz.NET, RabbitMQ | Custom |
| **API Type** | gRPC, REST, Both | gRPC |
| **Authentication** | API Key, JWT, Combined | API Key |

## 📐 Architecture

SmartMail follows **Clean Architecture** principles with clear separation of concerns.

**Technology Stack:**
- **.NET 6.0** (LTS)
- **SQL Server 2019+** with **Stored Procedures** (no ORM)
- **Dapper** for data access
- **Clean Architecture** + **DDD**
- **CQRS** with MediatR
- **gRPC** (default) + REST APIs

## 🏗️ Project Structure

```
SmartMail/
├── src/
│   ├── Core/
│   │   ├── SmartMail.Domain/              # Domain entities, value objects, events
│   │   └── SmartMail.Application/         # Use cases, DTOs, interfaces
│   ├── Infrastructure/
│   │   ├── SmartMail.Infrastructure/      # Email providers, schedulers, tracking
│   │   └── SmartMail.Persistence/         # Repositories, stored procedures, SQL scripts
│   │       ├── SQL/Schema/                # Database schema scripts
│   │       ├── SQL/StoredProcedures/      # All stored procedures
│   │       └── Repositories/              # Repository implementations (Dapper)
│   └── Presentation/
│       ├── SmartMail.API/                 # gRPC & REST APIs
│       └── SmartMail.Analytics.Web/       # Analytics dashboard (MVC + React)
└── tests/
    ├── SmartMail.Domain.Tests/
    ├── SmartMail.Application.Tests/
    └── SmartMail.Infrastructure.Tests/
```

## 🚦 Getting Started

### Prerequisites
- **.NET 6.0 SDK**
- **SQL Server 2019+** (or SQL Server Express)
- Visual Studio 2022 or VS Code
- SQL Server Management Studio (SSMS) - optional

### Quick Start

1. **Create the database and schema**
```bash
sqlcmd -S localhost -d master -i src/Infrastructure/SmartMail.Persistence/SQL/Schema/01_CreateSchema.sql
```

2. **Create stored procedures**
```bash
sqlcmd -S localhost -d SmartMailDb -i src/Infrastructure/SmartMail.Persistence/SQL/StoredProcedures/02_EmailStoredProcedures.sql
sqlcmd -S localhost -d SmartMailDb -i src/Infrastructure/SmartMail.Persistence/SQL/StoredProcedures/03_CampaignStoredProcedures.sql
sqlcmd -S localhost -d SmartMailDb -i src/Infrastructure/SmartMail.Persistence/SQL/StoredProcedures/04_TenantAndTrackingStoredProcedures.sql
```

3. **Update connection string** in `src/Presentation/SmartMail.API/appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SmartMailDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

4. **Run the API**
```bash
cd src/Presentation/SmartMail.API
dotnet run
```

See full documentation in `docs/` folder and [Migration Guide](docs/MIGRATION_GUIDE_NET6.md).

## ⚙️ Configuration

All components are switchable via `appsettings.json`. See documentation for details.

## 📚 Documentation

- [Architecture Documentation](docs/ARCHITECTURE.md)
- [Migration Guide (.NET 6.0 & Stored Procedures)](docs/MIGRATION_GUIDE_NET6.md)
- [Deployment Guide](docs/DEPLOYMENT_GUIDE.md)
- [Pros & Cons Analysis](docs/PROS_CONS_ANALYSIS.md)

## 📝 License

MIT License
