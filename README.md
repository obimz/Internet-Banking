# Internet Banking API

A REST API for an internet banking application, built with ASP.NET Core and SQL Server. Customers can register, log in, manage bank accounts, move money, save beneficiaries and view account statements.

> This project is for development and learning. It has not been through the security, compliance or financial-system reviews a real banking system needs.

## Table of Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [API Endpoints](#api-endpoints)
- [Running Tests](#running-tests)
- [Continuous Integration](#continuous-integration)
- [Project Structure](#project-structure)
- [Security Notes](#security-notes)

## Features

- **Authentication:** registration, login, change password and logout using JWT bearer tokens.
- **Roles:** `Customer` (default) and `Admin`. Some endpoints are admin-only.
- **Bank accounts:** every registration creates an account with a unique 10-digit account number.
- **Transactions:** deposit, withdraw, internal transfer between accounts, and a simulated domestic transfer.
- **Beneficiaries:** save, view, update and delete beneficiaries, and transfer to a saved beneficiary.
- **Account statements:** filter by date range and by credit or debit, with totals and net change.
- **Sample data:** in the Development environment the database is seeded with fake customers, accounts and transactions.

## Tech Stack

| Area | Technology |
|---|---|
| Framework | .NET 10, ASP.NET Core Web API |
| Database | SQL Server with Entity Framework Core |
| Authentication | JWT bearer tokens, ASP.NET Core Identity password hasher |
| API documentation | OpenAPI with Scalar |
| Seed data | Bogus |
| Testing | xUnit, FluentAssertions, FakeItEasy, EF Core InMemory |

## Getting Started

### Prerequisites

- [.NET 10 SDK]
- SQL Server (SQL Server Express or LocalDB is fine)
- The EF Core command-line tool:

  ```powershell
  dotnet tool install --global dotnet-ef
  ```

### Setup

1. Clone the repository and move into it:

   ```powershell
   git clone <repository-url>
   cd Internet-Banking
   ```

2. Set the JWT signing key (see [Configuration](#configuration)).

3. Check the connection string in `Internet-Banking/appsettings.json` points to your SQL Server instance.

4. Create the database:

   ```powershell
   dotnet restore
   dotnet ef database update --project .\Internet-Banking\Internet-Banking.csproj
   ```

5. Run the API:

   ```powershell
   dotnet run --project .\Internet-Banking\Internet-Banking.csproj
   ```

The API listens on `http://localhost:5173` (and `https://localhost:7238` with the `https` profile). In Development, the first start seeds the database with sample data, which can take a little while.

API documentation is available while running in Development:

- Scalar UI: `http://localhost:5173/scalar/v1`
- OpenAPI document: `http://localhost:5173/openapi/v1.json`

## Configuration

| Setting | Where it lives | Notes |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `appsettings.json` | SQL Server connection string |
| `Jwt:Issuer` | `appsettings.json` | Token issuer |
| `Jwt:Audience` | `appsettings.json` | Token audience |
| `Jwt:ExpiresInMinutes` | `appsettings.json` | Token lifetime, defaults to 60 |
| `Jwt:Key` | **User Secrets** | Signing key, at least 32 characters. Never commit it. |

Set the signing key for development with User Secrets:

```powershell
dotnet user-secrets set "Jwt:Key" "your-development-secret-key-at-least-32-characters" --project .\Internet-Banking\Internet-Banking.csproj
```

The app will not start without `Jwt:Key`.

### Seeded accounts

In Development the seeder creates 1,000 fake customers. The first one is an admin with the email `admin@internetbanking.com`. All seeded users share one default password, which is set in `Internet-Banking/Data/DataSeeders/CustomerSeeder.cs`.

### Database migrations

After changing an entity, create and apply a migration:

```powershell
dotnet ef migrations add MigrationName --project .\Internet-Banking\Internet-Banking.csproj
dotnet ef database update --project .\Internet-Banking\Internet-Banking.csproj
```

## API Endpoints

Endpoints marked "Yes" need an `Authorization: Bearer <token>` header. Get a token from `POST /api/auth/login`.

### Authentication

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | No | Create a customer and their first account |
| POST | `/api/auth/login` | No | Log in and receive a JWT |
| POST | `/api/auth/change-password` | Yes | Change the current user's password |
| POST | `/api/auth/logout` | Yes | Log out (the client discards the token) |

### Customers

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/api/customers/all-customer-details` | Admin | List all customers |
| GET | `/api/customers/specific-customer-details/{customerId}` | Yes | View one customer |
| PUT | `/api/customers/update-specific-customer-details/{customerId}` | Yes | Update a customer |

### Accounts

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/api/accounts/my-accounts` | Yes | List the current user's accounts |
| GET | `/api/accounts/account-details/{accountId}` | Yes | View an account |
| GET | `/api/accounts/account-balance/{accountId}` | Yes | View an account balance |
| GET | `/api/accounts/account-statement/{accountId}` | Yes | Statement with totals. Optional query: `startDate`, `endDate`, `transactionType` (`Credit` or `Debit`) |

### Transactions

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/transactions/deposit` | Yes | Deposit into one of your accounts |
| POST | `/api/transactions/withdraw` | Yes | Withdraw from one of your accounts |
| POST | `/api/transactions/internal-transfer` | Yes | Transfer between accounts in this bank |
| POST | `/api/transactions/domestic-transfer` | Yes | Simulated transfer to another bank |
| GET | `/api/transactions/view-transactions/{accountId}` | Yes | List transactions for an account |
| GET | `/api/transactions/view-details/{transactionId}` | Yes | View one transaction |

### Beneficiaries

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/beneficiaries/add-beneficiary` | Yes | Save a beneficiary by account number |
| GET | `/api/beneficiaries/view-beneficiaries` | Yes | List your saved beneficiaries |
| PUT | `/api/beneficiaries/update-beneficiary/{beneficiaryId}` | Yes | Update a beneficiary |
| DELETE | `/api/beneficiaries/delete-beneficiary/{beneficiaryId}` | Yes | Delete a beneficiary |
| POST | `/api/beneficiaries/transfer/{beneficiaryId}` | Yes | Transfer to a saved beneficiary |

### Example: log in

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@internetbanking.com",
  "password": "<seeded-password>"
}
```

Response:

```json
{ "token": "<jwt>" }
```

## Running Tests

From the repository root:

```powershell
dotnet test
```

The tests use an in-memory database, so no SQL Server is needed. They cover:

- Token creation (`TokenService`)
- Login and registration (`AuthController`)
- Transfer rules and balances (`TransactionService`)
- Deposit, withdraw and internal transfer endpoints (`TransactionsController`)

## Continuous Integration

A GitHub Actions workflow (`.github/workflows/ci.yml`) runs on every push to `main` and on every pull request into `main`. It restores packages, builds the solution in Release mode and runs the tests.

## Project Structure

```text
Internet-Banking/
├── .github/workflows/ci.yml        CI pipeline
├── Internet-Banking/               The API project
│   ├── Controllers/                HTTP endpoints
│   ├── Services/                   Business logic (accounts, tokens, transactions, statements)
│   ├── Models/                     Entities and enums
│   ├── DTOs/                       Request and response shapes
│   ├── Data/                       DbContext and data seeders
│   ├── Extensions/                 Startup helpers (database seeding)
│   ├── Migrations/                 EF Core migrations
│   ├── Program.cs                  App startup and dependency injection
│   └── appsettings.json
├── InternetBankingTests/           Unit tests
└── Internet-Banking.slnx           Solution file
```

## Security Notes

- Passwords are stored only as hashes, never as plain text.
- The JWT signing key must not be committed. Use User Secrets in development and a secret manager such as Azure Key Vault in production.
- Customers can only act on accounts they own. Transfers and withdrawals check ownership before moving money.
- The seeded accounts and their shared password are for development only. Do not seed or run this in production.
