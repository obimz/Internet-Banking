# Internet Banking API

An ASP.NET Core Web API for an internet banking application.

## Current technology

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT bearer authentication

## Current phase

Phase 1 focuses on customer authentication and profile operations:

- Customer registration
- Customer login
- View customer profile
- Update customer information
- Change password
- Logout

Bank accounts, transactions, and transfers will be implemented in later phases.

## Project structure

```text
Internet-Banking/
├── Controllers/
│   ├── AuthController.cs
│   └── CustomerController.cs
├── Data/
│   └── Internet_BankingDbContext.cs
├── Models/
│   ├── AccountStatus.cs
│   ├── AccountType.cs
│   ├── BankAccount.cs
│   └── Customer.cs
├── Migrations/
├── Program.cs
├── appsettings.json
└── Internet-Banking.csproj
```

## Configuration

The SQL Server connection string is configured using `DefaultConnection` in `appsettings.json`.

The JWT signing key must not be stored in source control. Configure it with User Secrets during development:

```powershell
dotnet user-secrets init --project ".\Internet-Banking\Internet-Banking.csproj"
dotnet user-secrets set "Jwt:Key" "your-development-secret-key-at-least-32-characters" --project ".\Internet-Banking\Internet-Banking.csproj"
```

The JWT issuer, audience, and expiration settings can be stored in `appsettings.json`. Keep the signing key in User Secrets or a production secret manager.

## Database migrations

From the repository root, restore packages and apply the database migrations:

```powershell
dotnet restore
dotnet ef database update --project ".\Internet-Banking\Internet-Banking\Internet-Banking.csproj" --startup-project ".\Internet-Banking\Internet-Banking\Internet-Banking.csproj"
```

To create a new migration after changing an entity:

```powershell
dotnet ef migrations add MigrationName --project ".\Internet-Banking\Internet-Banking\Internet-Banking.csproj" --startup-project ".\Internet-Banking\Internet-Banking\Internet-Banking.csproj"
```

## Running the application

```powershell
dotnet run --project ".\Internet-Banking\Internet-Banking\Internet-Banking.csproj"
```

The API can be tested using the configured `.http` file or an API client

## Phase 1 endpoints

### Authentication

```text
POST /api/auth/register
POST /api/auth/login
POST /api/auth/change-password
POST /api/auth/logout
```

Registration and login are anonymous endpoints. Password-protected operations require a valid JWT bearer token.

### Customer profile

```text
GET /api/customers/me
PUT /api/customers/me
```

These endpoints require a valid JWT bearer token.

## Security notes

- Passwords must never be stored as plain text; only password hashes are stored.
- JWT signing keys must not be committed to source control.
- Use a production secret manager such as Azure Key Vault when deploying.
- This project is for development and learning until it has undergone appropriate security, compliance, and financial-system reviews.
