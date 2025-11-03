# SmartMail Deployment Guide

## Prerequisites

- .NET 8.0 SDK
- SQL Server 2019+ or SQL Server Express
- Visual Studio 2022 or VS Code (optional but recommended)

## Step 1: Database Setup

### Option A: SQL Server (Recommended for Production)

1. Install SQL Server 2019 or later
2. Create a new database:
```sql
CREATE DATABASE SmartMailDb;
GO
```

3. Update connection string in `src/Presentation/SmartMail.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SmartMailDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### Option B: SQL Server Express (Free)

Download from: https://www.microsoft.com/en-us/sql-server/sql-server-downloads

Connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=SmartMailDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

## Step 2: Run Database Migrations

```bash
cd src/Presentation/SmartMail.API

# Add EF Core tools if not already installed
dotnet tool install --global dotnet-ef

# Run migrations
dotnet ef database update --project ../../Infrastructure/SmartMail.Persistence
```

## Step 3: Configure Email Provider

### SMTP Configuration (Default)

For Gmail:
```json
{
  "EmailProviders": {
    "Smtp": {
      "Enabled": true,
      "Host": "smtp.gmail.com",
      "Port": 587,
      "Username": "your-email@gmail.com",
      "Password": "your-app-password",
      "UseStartTls": true
    }
  }
}
```

**Note**: For Gmail, you need to create an "App Password":
1. Go to Google Account Settings
2. Security → 2-Step Verification
3. App Passwords → Generate new password

### SendGrid (Production Recommended)

```json
{
  "EmailService": {
    "DefaultProvider": "SendGrid"
  },
  "EmailProviders": {
    "SendGrid": {
      "Enabled": true,
      "ApiKey": "YOUR_SENDGRID_API_KEY"
    }
  }
}
```

## Step 4: Create First Tenant

Run this SQL to create your first tenant:

```sql
INSERT INTO Tenants (Id, TenantId, Name, IsActive, DefaultEmailProvider,
    MaxAttachmentSizeInMb, MaxEmailsPerHour, MaxEmailsPerDay,
    EnableOpenTracking, EnableClickTracking, ApiKey, ApiKeyCreatedAt, CreatedAt)
VALUES (
    NEWID(),
    NEWID(),
    'Your Company',
    1,
    'Smtp',
    25,
    1000,
    10000,
    1,
    1,
    'test-api-key-change-in-production',
    GETUTCDATE(),
    GETUTCDATE()
);

-- Get your Tenant ID and API Key
SELECT TenantId, Name, ApiKey FROM Tenants;
```

Save the TenantId and ApiKey - you'll need them for API calls.

## Step 5: Run the Application

```bash
cd src/Presentation/SmartMail.API
dotnet run
```

The API will start on:
- gRPC: https://localhost:5001
- REST: https://localhost:5003 (if enabled)

## Step 6: Test the API

### Using gRPC

Create a test client (or use existing .NET project):

```bash
dotnet new console -n SmartMailTestClient
cd SmartMailTestClient
dotnet add package Grpc.Net.Client
dotnet add package Google.Protobuf
dotnet add package Grpc.Tools
```

Copy the proto files from `src/Presentation/SmartMail.API/Protos/` to your client project.

Test code:
```csharp
using Grpc.Net.Client;
using SmartMail.API.Protos;

var channel = GrpcChannel.ForAddress("https://localhost:5001");
var client = new SmartMailService.SmartMailServiceClient(channel);

var request = new SendEmailRequest
{
    TenantId = "YOUR_TENANT_ID",
    From = "sender@example.com",
    To = "recipient@example.com",
    Subject = "Test Email",
    HtmlBody = "<h1>Hello from SmartMail!</h1>",
    SendImmediately = true
};

// Add API key to metadata
var headers = new Metadata
{
    { "X-API-Key", "YOUR_API_KEY" }
};

var response = await client.SendEmailAsync(request, headers);
Console.WriteLine($"Success: {response.Success}");
Console.WriteLine($"Email ID: {response.EmailId}");
```

## Production Deployment Checklist

### Security
- [ ] Change default API keys
- [ ] Use strong SQL Server password
- [ ] Enable HTTPS with valid certificate
- [ ] Configure CORS if using REST API
- [ ] Set JWT secret to strong random value
- [ ] Enable SQL Server TDE (encryption at rest)
- [ ] Configure firewall rules

### Performance
- [ ] Enable SQL Server connection pooling
- [ ] Configure appropriate rate limits
- [ ] Set up database indexes (auto-created by migrations)
- [ ] Configure logging levels (Warning or Error in production)
- [ ] Enable response compression

### Monitoring
- [ ] Set up application logging
- [ ] Configure Serilog file sinks
- [ ] Set up SQL Server monitoring
- [ ] Configure health checks
- [ ] Set up alerts for failures

### Email Configuration
- [ ] Choose production email provider
- [ ] Configure SPF, DKIM, DMARC records
- [ ] Verify sender domain
- [ ] Test deliverability
- [ ] Configure bounce handling webhooks

### Backup
- [ ] Set up SQL Server backup schedule
- [ ] Test backup restoration
- [ ] Document recovery procedures

## Docker Deployment

### Create Dockerfile

```dockerfile
# src/Presentation/SmartMail.API/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5001
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Presentation/SmartMail.API/SmartMail.API.csproj", "SmartMail.API/"]
COPY ["src/Core/SmartMail.Application/SmartMail.Application.csproj", "SmartMail.Application/"]
COPY ["src/Core/SmartMail.Domain/SmartMail.Domain.csproj", "SmartMail.Domain/"]
COPY ["src/Infrastructure/SmartMail.Infrastructure/SmartMail.Infrastructure.csproj", "SmartMail.Infrastructure/"]
COPY ["src/Infrastructure/SmartMail.Persistence/SmartMail.Persistence.csproj", "SmartMail.Persistence/"]

RUN dotnet restore "SmartMail.API/SmartMail.API.csproj"
COPY src/ .
WORKDIR "/src/SmartMail.API"
RUN dotnet build "SmartMail.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartMail.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartMail.API.dll"]
```

### Create docker-compose.yml

```yaml
version: '3.8'

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong!Passw0rd
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql

  smartmail-api:
    build:
      context: .
      dockerfile: src/Presentation/SmartMail.API/Dockerfile
    ports:
      - "5001:5001"
      - "5003:5003"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+:5001;http://+:5003
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=SmartMailDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True
    depends_on:
      - sqlserver

volumes:
  sqldata:
```

### Run with Docker

```bash
docker-compose up -d
```

## Windows Service Deployment

To run as a Windows Service:

```bash
# Publish the application
dotnet publish -c Release -o C:\Services\SmartMail

# Install as Windows Service (requires admin)
sc create SmartMailService binPath="C:\Services\SmartMail\SmartMail.API.exe"
sc start SmartMailService
```

## IIS Deployment

1. Publish the application:
```bash
dotnet publish -c Release -o C:\inetpub\wwwroot\SmartMail
```

2. Create new IIS Application Pool (.NET CLR version: No Managed Code)
3. Create new IIS Website pointing to published folder
4. Configure HTTPS binding with certificate

## Troubleshooting

### Database Connection Issues
- Verify SQL Server is running
- Check connection string is correct
- Ensure SQL Server allows remote connections
- Check firewall allows port 1433

### Email Not Sending
- Check email provider credentials
- Verify SMTP port is not blocked
- Check logs for error messages
- Test SMTP connection with telnet

### gRPC Connection Issues
- Ensure HTTP/2 is enabled
- Check certificate is valid
- Verify firewall allows gRPC port
- Check client is using correct TLS settings

---

**Need Help?** Check the logs in the application directory or SQL Server error logs.
