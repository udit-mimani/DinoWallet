# 🦕 LedgerX - Virtual Wallet Service

A production-ready internal wallet service for managing virtual credits (Gold Coins, Diamonds, Loyalty Points) with **double-entry bookkeeping**, **idempotency guarantees**, and **deadlock-free concurrency**.

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## 🌐 Live Demo

The service is deployed on **Render** (free tier) with a managed **PostgreSQL** database.

| | URL |
|---|---|
| 🏠 Base URL | https://ledgerx.onrender.com |
| 📖 Swagger UI | https://ledgerx.onrender.com/swagger |
| ❤️ Health Check | https://ledgerx.onrender.com/health |

> **Note:** This is hosted on Render's free tier. The first request after a period of inactivity
> may take **20–30 seconds** to wake the instance. Subsequent requests are fast.
> The database is always on and pre-seeded with asset types, treasury accounts, Alice, and Bob.

### 🧪 Try It Instantly (No Setup Required)

Open the [Swagger UI](https://ledgerx.onrender.com/swagger) and run these in order:

**1. Check seeded accounts**
```
GET /api/accounts
```

**2. Check Alice's balance**
```
GET /api/accounts/{aliceId}/balance
```

**3. Top-up Alice's wallet**
```
POST /api/wallet/topup
{
  "accountId": "<aliceId>",
  "treasuryAccountId": "<treasuryId>",
  "amount": 100,
  "idempotencyKey": "demo-topup-001",
  "description": "Purchased 100 Gold Coins"
}
```

**4. Give Alice a bonus**
```
POST /api/wallet/bonus
{
  "accountId": "<aliceId>",
  "amount": 50,
  "idempotencyKey": "demo-bonus-001",
  "description": "Referral bonus"
}
```

**5. Alice spends credits**
```
POST /api/wallet/spend
{
  "accountId": "<aliceId>",
  "amount": 30,
  "idempotencyKey": "demo-spend-001",
  "description": "Purchased Premium Sword"
}
```

**6. View Alice's full ledger**
```
GET /api/accounts/{aliceId}/ledger
```

> 💡 Use the **Postman collection** in `/postman` for a pre-wired end-to-end test suite with
> environment variables already configured for the live URL.

---

## 📋 Table of Contents

- [Features](#-features)
- [Architecture](#-architecture)
- [Quick Start](#-quick-start)
- [API Reference](#-api-reference)
- [Domain Model](#-domain-model)
- [Concurrency & Safety](#-concurrency--safety)
- [Testing](#-testing)
- [Configuration](#-configuration)
- [Postman Collection](#-postman-collection)
- [Deployment](#-deployment)
- [Contributing](#-contributing)

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| 🔄 **Double-Entry Bookkeeping** | Every transaction creates exactly 2 ledger entries (debit + credit). Balance = SUM of entries. |
| 🔒 **Idempotency** | All write operations support idempotency keys - safe to retry without duplicates |
| ⚡ **Deadlock-Free** | Advisory locks acquired in deterministic order prevent circular waits |
| 🏦 **Multi-Asset Support** | Gold Coins, Diamonds, Loyalty Points - easily extensible |
| 📊 **Audit Trail** | Complete ledger history with pagination |
| 🐳 **Docker Ready** | Production-ready with Docker Compose |
| ✅ **Integration Tests** | Testcontainers-based tests against real PostgreSQL |

---

## 🏗 Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         LedgerX API                              │
├─────────────────────────────────────────────────────────────────────┤
│  Controllers                                                        │
│  ├── WalletController     POST /api/wallet/topup|bonus|spend       │
│  └── AccountsController   GET  /api/accounts, balance, ledger      │
├─────────────────────────────────────────────────────────────────────┤
│  Services                                                           │
│  ├── WalletService        Core business logic + concurrency        │
│  └── DatabaseSeeder       Initial data setup                       │
├─────────────────────────────────────────────────────────────────────┤
│  Domain Models                                                      │
│  ├── Account              User/Treasury wallet                     │
│  ├── AssetType            GLD, DIA, LPT                            │
│  ├── Transaction          TopUp, Bonus, Spend                      │
│  ├── LedgerEntry          Debit/Credit lines                       │
│  └── IdempotencyRecord    Duplicate request protection             │
├─────────────────────────────────────────────────────────────────────┤
│  Infrastructure                                                     │
│  ├── WalletDbContext      EF Core + PostgreSQL                     │
│  └── GlobalExceptionMiddleware  Unified error handling             │
└─────────────────────────────────────────────────────────────────────┘
```

### Transaction Flows

```
TopUp/Bonus:  Treasury ──(-amount)──► User Account ──(+amount)──►
              (debit)                 (credit)

Spend:        User Account ──(-amount)──► Treasury ──(+amount)──►
              (debit)                     (credit)
```

---

## 🚀 Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started) (for containerized deployment)
- [PostgreSQL 16](https://www.postgresql.org/) (or use Docker)

### Option 1: Docker Compose (Recommended)

```bash
# Clone the repository
git clone https://github.com/udit-mimani/LedgerX.git
cd LedgerX

# Start all services
docker-compose up -d

# View logs
docker-compose logs -f api

# API available at http://localhost:8080
# Swagger UI: http://localhost:8080/swagger
```

### Option 2: Local Development

```bash
# Clone the repository
git clone https://github.com/udit-mimani/LedgerX.git
cd LedgerX

# Start PostgreSQL (if not running)
docker run -d --name postgres -p 5432:5432 \
  -e POSTGRES_USER=wallet \
  -e POSTGRES_PASSWORD=wallet123 \
  -e POSTGRES_DB=ledgerx \
  postgres:16

# Run the API
cd src/LedgerX.Api
dotnet run

# API available at http://localhost:5000
# Swagger UI: http://localhost:5000/swagger
```

### Option 3: Podman

```bash
# Build without cache
podman-compose build --no-cache

# Start services
podman-compose up -d

# View logs
podman logs ledgerx-api -f
```

---

## 📖 API Reference

### Health Check

```http
GET /health
```

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Accounts

#### List All Accounts
```http
GET /api/accounts
```

#### Get Account Balance
```http
GET /api/accounts/{accountId}/balance
```

**Response:**
```json
{
  "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
  "accountName": "Alice",
  "assetType": "Gold Coins",
  "assetSymbol": "GLD",
  "balance": 500.00
}
```

#### Get Ledger History
```http
GET /api/accounts/{accountId}/ledger?skip=0&take=20
```

**Response:**
```json
{
  "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
  "accountName": "Alice",
  "runningBalance": 500.00,
  "totalEntries": 5,
  "skip": 0,
  "take": 20,
  "entries": [
    {
      "id": 10,
      "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
      "accountName": "Alice",
      "amount": 100.00,
      "transactionId": 5,
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ]
}
```

### Wallet Operations

#### Top-Up (Purchase Credits)
```http
POST /api/wallet/topup
Content-Type: application/json

{
  "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
  "treasuryAccountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
  "amount": 100,
  "idempotencyKey": "purchase-2024-001",
  "description": "Purchased 100 Gold Coins"
}
```

#### Bonus (Free Credits)
```http
POST /api/wallet/bonus
Content-Type: application/json

{
  "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
  "amount": 50,
  "idempotencyKey": "daily-reward-2024-01-15",
  "description": "Daily login reward"
}
```

#### Spend (Use Credits)
```http
POST /api/wallet/spend
Content-Type: application/json

{
  "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
  "amount": 30,
  "idempotencyKey": "purchase-sword-001",
  "description": "Purchased Premium Sword"
}
```

**Success Response (all wallet operations):**
```json
{
  "transactionId": 5,
  "type": "TopUp",
  "description": "Purchased 100 Gold Coins",
  "idempotencyKey": "purchase-2024-001",
  "createdAt": "2024-01-15T10:30:00Z",
  "entries": [
    {
      "id": 9,
      "accountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
      "accountName": "Treasury - Gold Coins",
      "amount": -100.00,
      "transactionId": 5,
      "createdAt": "2024-01-15T10:30:00Z"
    },
    {
      "id": 10,
      "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
      "accountName": "Alice",
      "amount": 100.00,
      "transactionId": 5,
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ]
}
```

### Error Responses

| Status Code | Error | Description |
|-------------|-------|-------------|
| `400` | Bad Request | Invalid amount (zero or negative) |
| `404` | Not Found | Account not found |
| `422` | Unprocessable Entity | Insufficient funds for Spend |
| `500` | Internal Server Error | Unexpected server error |

**Error Response Format:**
```json
{
  "error": "Insufficient funds. Balance: 50.00, Requested: 100.00",
  "detail": "Balance: 50.00, Requested: 100.00"
}
```

---

## 🗃 Domain Model

### Entity Relationship

```
AssetType (1) ──────────< Account (N)
    │                        │
    │                        │
    └── GLD, DIA, LPT        ├── System (Treasury)
                             └── User (Alice, Bob)

Account (1) ────────────< LedgerEntry (N) >──────────── Transaction (1)
                              │
                              └── Amount (+credit / -debit)

Transaction (1) ─────────── IdempotencyRecord (1)
                              │
                              └── Key (unique)
```

### Account Types

| Type | Purpose | Example |
|------|---------|---------|
| `System` | Treasury accounts (unlimited funds source) | Treasury - Gold Coins |
| `User` | User wallets | Alice, Bob |

### Transaction Types

| Type | Flow | Use Case |
|------|------|----------|
| `TopUp` | Treasury → User | User purchases credits with real money |
| `Bonus` | Treasury → User | Free credits (referral, daily reward) |
| `Spend` | User → Treasury | User spends credits in-app |

### Asset Types (Seeded)

| Symbol | Name | Description |
|--------|------|-------------|
| `GLD` | Gold Coins | Primary virtual currency |
| `DIA` | Diamonds | Premium currency |
| `LPT` | Loyalty Points | Reward points |

---

## 🔐 Concurrency & Safety

The wallet service implements a **deadlock-free, race-condition-safe** protocol:

### 10-Step Transaction Protocol

```
1. Fast idempotency pre-check (no lock - fast path for duplicates)
2. Resolve accounts (read-only lookups)
3. BEGIN TRANSACTION (ReadCommitted isolation)
4. Acquire pg_advisory_xact_lock in ascending UUID order (deadlock prevention)
5. Re-check idempotency inside transaction (TOCTOU race guard)
6. SELECT ... FOR UPDATE on account rows (pessimistic locking)
7. Validate balance for Spend (guaranteed to see committed state)
8. INSERT Transaction + 2× LedgerEntry (double-entry bookkeeping)
9. INSERT IdempotencyRecord with TransactionId
10. COMMIT
```

### Why This Works

| Mechanism | Purpose |
|-----------|---------|
| **Advisory Locks** | Deterministic lock ordering (ascending UUID) prevents deadlocks |
| **FOR UPDATE** | Row-level locks ensure balance reads are not stale |
| **Double Idempotency Check** | Pre-check (fast path) + inner-check (race guard) |
| **Execution Strategy** | Supports EF Core retry-on-failure with manual transactions |

---

## 🧪 Testing

### Run All Tests

```bash
dotnet test
```

### Integration Tests

Integration tests use [Testcontainers](https://testcontainers.com/) to spin up a real PostgreSQL instance:

```bash
cd tests/LedgerX.IntegrationTests
dotnet test
```

**Test Coverage:**

| Test | Description |
|------|-------------|
| `TopUp_CreditBalance` | Verifies balance increases after TopUp |
| `Spend_DebitBalance` | Verifies balance decreases after Spend |
| `Spend_InsufficientFunds_Returns422` | Validates insufficient funds handling |
| `Idempotency_SameKey_ReturnsSameTransaction` | Ensures duplicate requests are idempotent |
| `ConcurrentTransfers_NoDeadlock` | Validates deadlock-free concurrent operations |
| `GetBalance_AccountNotFound_Returns404` | Validates 404 for non-existent accounts |

### Test Output

```
Test summary: total: 16, failed: 0, succeeded: 16, skipped: 0
```

---

## ⚙️ Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | See appsettings.json |
| `ASPNETCORE_ENVIRONMENT` | Environment (Development/Production) | Development |

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ledgerx;Username=wallet;Password=wallet123"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### Docker Compose Environment

```yaml
environment:
  - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=ledgerx;Username=wallet;Password=wallet123
  - ASPNETCORE_ENVIRONMENT=Development
```

---

## 📬 Postman Collection

A complete Postman collection is included for testing all API endpoints.

### Files

| File | Description |
|------|-------------|
| `postman/LedgerX.postman_collection.json` | Full API collection with tests |
| `postman/LedgerX.Local.postman_environment.json` | Local environment (port 5000) |
| `postman/LedgerX.Docker.postman_environment.json` | Docker environment (port 8080) |

### Import & Setup

1. **Import Collection:**
   - Open Postman → Import → Select `postman/LedgerX.postman_collection.json`

2. **Import Environment:**
   - Import `postman/LedgerX.Local.postman_environment.json` or
   - Import `postman/LedgerX.Docker.postman_environment.json`

3. **Select Environment:**
   - Choose the appropriate environment from the dropdown

4. **Run Setup:**
   - Execute **"1. Get All Accounts"** first to auto-populate account IDs

### Collection Structure

```
LedgerX API
├── Health & Setup
│   └── Health Check
├── Accounts
│   ├── 1. Get All Accounts        ← Run this first!
│   ├── 2. Get Alice's Balance
│   ├── 3. Get Bob's Balance
│   ├── 4. Get Alice's Ledger
│   └── 5. Get Balance - 404
├── Wallet Operations
│   ├── TopUp
│   │   ├── 1. TopUp Alice - Success
│   │   ├── 2. TopUp - Idempotent Replay
│   │   ├── 3. TopUp - Zero Amount (400)
│   │   ├── 4. TopUp - Negative Amount (400)
│   │   └── 5. TopUp - Non-Existent Account (404)
│   ├── Bonus
│   │   ├── 1. Bonus to Bob
│   │   └── 2. Bonus to Alice - Daily Reward
│   └── Spend
│       ├── 1. Spend - Alice Buys Item
│       ├── 2. Spend - Insufficient Funds (422)
│       └── 3. Spend - Bob Unlocks Level
└── End-to-End Scenarios
    └── Complete User Journey (6 steps)
```

---

## 📁 Project Structure

```
LedgerX/
├── src/
│   └── LedgerX.Api/
│       ├── Controllers/
│       │   ├── AccountsController.cs
│       │   └── WalletController.cs
│       ├── Data/
│       │   └── WalletDbContext.cs
│       ├── Domain/
│       │   ├── Account.cs
│       │   ├── AssetType.cs
│       │   ├── IdempotencyRecord.cs
│       │   ├── LedgerEntry.cs
│       │   └── Transaction.cs
│       ├── DTOs/
│       │   ├── Requests/
│       │   │   └── WalletRequests.cs
│       │   └── Responses/
│       │       └── WalletResponses.cs
│       ├── Exceptions/
│       │   └── WalletExceptions.cs
│       ├── Middleware/
│       │   └── GlobalExceptionMiddleware.cs
│       ├── Migrations/
│       ├── Services/
│       │   ├── DatabaseSeeder.cs
│       │   ├── IWalletService.cs
│       │   └── WalletService.cs
│       ├── Program.cs
│       └── appsettings.json
├── tests/
│   └── LedgerX.IntegrationTests/
│       ├── WalletApiTests.cs
│       └── WalletApiFixture.cs
├── postman/
│   ├── LedgerX.postman_collection.json
│   ├── LedgerX.Local.postman_environment.json
│   └── LedgerX.Docker.postman_environment.json
├── docker-compose.yml
├── Dockerfile
├── LedgerX.sln
└── README.md
```

---

## ☁️ Deployment

This project is deployed using **Render** (Web Service + PostgreSQL).

### Infrastructure

| Component | Service | Plan |
|---|---|---|
| API | Render Web Service (Docker) | Free |
| Database | Render PostgreSQL | Free |

### How It Works

- Render builds the Docker image directly from the `Dockerfile` in the repo root
- On every startup, the app automatically runs **EF Core migrations** and the **DatabaseSeeder** — no manual SQL needed
- The database is pre-seeded with asset types (GLD, DIA, LPT), treasury accounts, and two users (Alice, Bob)
- Deployments are triggered automatically on every push to `main`

### Environment Variables (set on Render)

| Variable | Description |
|---|---|
| `ConnectionStrings__DefaultConnection` | Render PostgreSQL internal connection string (Npgsql format) |

### Re-deploying Yourself

If you want to deploy your own instance:

1. Fork this repo
2. Create a **PostgreSQL** database on [Render](https://render.com) (free tier)
3. Create a **Web Service** on Render, connect your fork, set runtime to **Docker**
4. Add the environment variable `ConnectionStrings__DefaultConnection` with the Npgsql-formatted connection string:
   ```
   Host=<host>;Database=<db>;Username=<user>;Password=<pass>;SSL Mode=Require;Trust Server Certificate=true
   ```
5. Deploy — migrations and seeding run automatically on first boot

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines

- Follow C# coding conventions
- Write integration tests for new features
- Update README for API changes
- Use meaningful commit messages

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [Npgsql](https://www.npgsql.org/) - .NET PostgreSQL provider
- [Testcontainers](https://testcontainers.com/) - Integration testing
- [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore) - Swagger/OpenAPI

---

<p align="center">
  Made with 🦕 by <a href="https://github.com/udit-mimani">Udit Mimani</a>
</p>
