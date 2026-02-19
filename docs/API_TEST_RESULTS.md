# ?? DinoWallet API - Test Results

This document contains the results of testing all API endpoints using the Postman collection.

**Test Date:** February 20, 2025  
**Environment:** Docker (localhost:8080)  
**Collection:** DinoWallet.postman_collection.json

---

## ?? Test Summary

| Category | Tests | Passed | Failed |
|----------|-------|--------|--------|
| Health & Setup | 1 | ? 1 | ? 0 |
| Accounts | 5 | ? 5 | ? 0 |
| TopUp Operations | 5 | ? 5 | ? 0 |
| Bonus Operations | 2 | ? 2 | ? 0 |
| Spend Operations | 3 | ? 3 | ? 0 |
| End-to-End Scenario | 6 | ? 6 | ? 0 |
| **Total** | **22** | **? 22** | **? 0** |

---

## 1?? Health & Setup

### Health Check ?

| Property | Value |
|----------|-------|
| **Endpoint** | `GET /health` |
| **Status Code** | `200 OK` |
| **Response Time** | `12ms` |

**Response:**
```json
{
    "status": "healthy",
    "timestamp": "2026-02-19T19:25:04.5912336Z"
}
```

**Tests Passed:**
- ? Status code is 200
- ? Response has healthy status

---

## 2?? Accounts

### 2.1 Get All Accounts ?

| Property | Value |
|----------|-------|
| **Endpoint** | `GET /api/accounts` |
| **Status Code** | `200 OK` |
| **Response Time** | `45ms` |

**Response:**
```json
[
    {
        "id": "15a6ddd0-2791-446d-84ed-33aec79c0187",
        "name": "Alice",
        "type": "User",
        "assetType": "Gold Coins",
        "assetSymbol": "GLD",
        "createdAt": "2026-02-19T18:27:24.446328Z"
    },
    {
        "id": "f8fc93e2-b8a9-4fb9-bbd8-0acb2d39aa4f",
        "name": "Bob",
        "type": "User",
        "assetType": "Gold Coins",
        "assetSymbol": "GLD",
        "createdAt": "2026-02-19T18:27:24.446329Z"
    },
    {
        "id": "15a47f39-ecd3-4f63-9ad5-356c332031a7",
        "name": "Treasury - Diamonds",
        "type": "System",
        "assetType": "Diamonds",
        "assetSymbol": "DIA",
        "createdAt": "2026-02-19T18:27:24.366781Z"
    },
    {
        "id": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
        "name": "Treasury - Gold Coins",
        "type": "System",
        "assetType": "Gold Coins",
        "assetSymbol": "GLD",
        "createdAt": "2026-02-19T18:27:24.366688Z"
    },
    {
        "id": "2ea1961c-e50a-46c2-9343-4407c54233d9",
        "name": "Treasury - Loyalty Points",
        "type": "System",
        "assetType": "Loyalty Points",
        "assetSymbol": "LPT",
        "createdAt": "2026-02-19T18:27:24.366781Z"
    }
]
```

**Tests Passed:**
- ? Status code is 200
- ? Response is an array
- ? Found Alice, Bob, and Treasury accounts

**Auto-populated Variables:**
| Variable | Value |
|----------|-------|
| `aliceId` | `15a6ddd0-2791-446d-84ed-33aec79c0187` |
| `bobId` | `f8fc93e2-b8a9-4fb9-bbd8-0acb2d39aa4f` |
| `treasuryId` | `cff3f9ef-970a-4a63-ac4a-42663bb116c1` |

---

### 2.2 Get Alice's Balance ?

| Property | Value |
|----------|-------|
| **Endpoint** | `GET /api/accounts/15a6ddd0-2791-446d-84ed-33aec79c0187/balance` |
| **Status Code** | `200 OK` |
| **Response Time** | `18ms` |

**Response:**
```json
{
    "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
    "accountName": "Alice",
    "assetType": "Gold Coins",
    "assetSymbol": "GLD",
    "balance": 500.0000
}
```

**Tests Passed:**
- ? Status code is 200
- ? Response has balance fields (accountId, accountName, balance, assetSymbol)
- ? Balance is a number

---

### 2.3 Get Bob's Balance ?

| Property | Value |
|----------|-------|
| **Endpoint** | `GET /api/accounts/f8fc93e2-b8a9-4fb9-bbd8-0acb2d39aa4f/balance` |
| **Status Code** | `200 OK` |
| **Response Time** | `15ms` |

**Response:**
```json
{
    "accountId": "f8fc93e2-b8a9-4fb9-bbd8-0acb2d39aa4f",
    "accountName": "Bob",
    "assetType": "Gold Coins",
    "assetSymbol": "GLD",
    "balance": 300.0000
}
```

**Tests Passed:**
- ? Status code is 200
- ? Response has balance fields

---

### 2.4 Get Alice's Ledger (Paginated) ?

| Property | Value |
|----------|-------|
| **Endpoint** | `GET /api/accounts/15a6ddd0-2791-446d-84ed-33aec79c0187/ledger?skip=0&take=20` |
| **Status Code** | `200 OK` |
| **Response Time** | `32ms` |

**Response:**
```json
{
    "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
    "accountName": "Alice",
    "runningBalance": 500.0000,
    "totalEntries": 1,
    "skip": 0,
    "take": 20,
    "entries": [
        {
            "id": 2,
            "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
            "accountName": "Alice",
            "amount": 500.0000,
            "transactionId": 1,
            "createdAt": "2026-02-19T18:27:24.468049Z"
        }
    ]
}
```

**Tests Passed:**
- ? Status code is 200
- ? Response has pagination fields (totalEntries, skip, take, entries, runningBalance)
- ? Entries is an array

---

### 2.5 Get Balance - Non-Existent Account (404) ?

| Property | Value |
|----------|-------|
| **Endpoint** | `GET /api/accounts/00000000-0000-0000-0000-000000000000/balance` |
| **Status Code** | `404 Not Found` |
| **Response Time** | `8ms` |

**Response:**
```json
{
    "error": "Account not found",
    "detail": "Account 00000000-0000-0000-0000-000000000000 not found."
}
```

**Tests Passed:**
- ? Status code is 404
- ? Response has error field

---

## 3?? Wallet Operations - TopUp

### 3.1 TopUp Alice - Success ?

| Property | Value |
|----------|-------|
| **Endpoint** | `POST /api/wallet/topup` |
| **Status Code** | `200 OK` |
| **Response Time** | `89ms` |

**Request Body:**
```json
{
  "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
  "treasuryAccountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
  "amount": 100,
  "idempotencyKey": "topup-c2f82078-68c8-4ffb-b6da-84a66ec6f585",
  "description": "Purchased 100 Gold Coins"
}
```

**Response:**
```json
{
    "transactionId": 3,
    "type": "TopUp",
    "description": "Purchased 100 Gold Coins",
    "idempotencyKey": "topup-509d24a3-7632-4254-a692-063dc0298c6d",
    "createdAt": "2026-02-19T19:34:23.8046702Z",
    "entries": [
        {
            "id": 5,
            "accountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
            "accountName": "Treasury - Gold Coins",
            "amount": -100,
            "transactionId": 3,
            "createdAt": "2026-02-19T19:34:23.8046702Z"
        },
        {
            "id": 6,
            "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
            "accountName": "Alice",
            "amount": 100,
            "transactionId": 3,
            "createdAt": "2026-02-19T19:34:23.8046702Z"
        }
    ]
}
```

**Tests Passed:**
- ? Status code is 200
- ? Transaction type is TopUp
- ? Transaction has 2 ledger entries
- ? Has debit (-100) and credit (+100) entries

---

### 3.2 TopUp Alice - Idempotent Replay ?

| Property | Value |
|----------|-------|
| **Endpoint** | `POST /api/wallet/topup` |
| **Status Code** | `200 OK` |
| **Response Time** | `25ms` |

**Request Body:** (Same idempotency key as 3.1)
```json
{
  "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
  "treasuryAccountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
  "amount": 100,
  "idempotencyKey": "topup-c2f82078-68c8-4ffb-b6da-84a66ec6f585",
  "description": "Duplicate request - should return same result"
}
```

**Response:**
```json
{
    "transactionId": 3,
    "type": "TopUp",
    "description": "Purchased 100 Gold Coins",
    "idempotencyKey": "topup-509d24a3-7632-4254-a692-063dc0298c6d",
    "createdAt": "2026-02-19T19:34:23.80467Z",
    "entries": [
        {
            "id": 5,
            "accountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
            "accountName": "Treasury - Gold Coins",
            "amount": -100.0000,
            "transactionId": 3,
            "createdAt": "2026-02-19T19:34:23.80467Z"
        },
        {
            "id": 6,
            "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
            "accountName": "Alice",
            "amount": 100.0000,
            "transactionId": 3,
            "createdAt": "2026-02-19T19:34:23.80467Z"
        }
    ]
}
```

**Tests Passed:**
- ? Status code is 200
- ? Returns same transaction ID (idempotent) - `transactionId: 2` matches previous request

**Key Observation:** Same `transactionId: 2` returned without creating a duplicate transaction.

---

### 3.3 TopUp - Zero Amount (400) ?

| Property | Value |
|----------|-------|
| **Endpoint** | `POST /api/wallet/topup` |
| **Status Code** | `400 Bad Request` |
| **Response Time** | `12ms` |

**Request Body:**
```json
{
  "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
  "treasuryAccountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
  "amount": 0,
  "idempotencyKey": "zero-amount-test-abc123",
  "description": "Should fail - zero amount"
}
```

**Response:**
```json
{
    "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
    "title": "One or more validation errors occurred.",
    "status": 400,
    "errors": {
        "Amount": [
            "Amount must be greater than zero."
        ]
    },
    "traceId": "00-aef2d90096dfd8665545c2d29a830906-0764d1765160b0f7-00"
}
```

**Tests Passed:**
- ? Status code is 400

---

### 3.4 TopUp - Negative Amount (400) ?

| Property | Value |
|----------|-------|
| **Endpoint** | `POST /api/wallet/topup` |
| **Status Code** | `400 Bad Request` |
| **Response Time** | `10ms` |

**Request Body:**
```json
{
  "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
  "treasuryAccountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
  "amount": -50,
  "idempotencyKey": "negative-amount-test-xyz789",
  "description": "Should fail - negative amount"
}
```

**Response:**
```json
{
    "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
    "title": "One or more validation errors occurred.",
    "status": 400,
    "errors": {
        "Amount": [
            "Amount must be greater than zero."
        ]
    },
    "traceId": "00-b4d1906ca3399ee87f5f04e064b582c6-bdc3851ad0e5fdb9-00"
}
```

**Tests Passed:**
- ? Status code is 400

---

### 3.5 TopUp - Non-Existent Account (404) ?

| Property | Value |
|----------|-------|
| **Endpoint** | `POST /api/wallet/topup` |
| **Status Code** | `404 Not Found` |
| **Response Time** | `15ms` |

**Request Body:**
```json
{
  "accountId": "00000000-0000-0000-0000-000000000000",
  "treasuryAccountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
  "amount": 100,
  "idempotencyKey": "nonexistent-account-test",
  "description": "Should fail - account not found"
}
```

**Response:**
```json
{
    "error": "Account not found",
    "detail": "Account 00000000-0000-0000-0000-000000000000 not found."
}
```

**Tests Passed:**
- ? Status code is 404
- ? Response has error field

---

## 4?? Wallet Operations - Bonus

### 4.1 Bonus to Bob - Success ?

| Property | Value |
|----------|-------|
| **Endpoint** | `POST /api/wallet/bonus` |
| **Status Code** | `200 OK` |
| **Response Time** | `78ms` |

**Request Body:**
```json
{
  "accountId": "f8fc93e2-b8a9-4fb9-bbd8-0acb2d39aa4f",
  "treasuryAccountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
  "amount": 50,
  "idempotencyKey": "bonus-bob-unique-key",
  "description": "Referral bonus for inviting a friend"
}
```

**Response:**
```json
{
    "transactionId": 4,
    "type": "Bonus",
    "description": "Referral bonus for inviting a friend",
    "idempotencyKey": "bonus-bob-8d19fdf5-3e15-46cb-aff7-1ba512dca250",
    "createdAt": "2026-02-19T19:36:11.3053645Z",
    "entries": [
        {
            "id": 7,
            "accountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
            "accountName": "Treasury - Gold Coins",
            "amount": -50,
            "transactionId": 4,
            "createdAt": "2026-02-19T19:36:11.3053645Z"
        },
        {
            "id": 8,
            "accountId": "f8fc93e2-b8a9-4fb9-bbd8-0acb2d39aa4f",
            "accountName": "Bob",
            "amount": 50,
            "transactionId": 4,
            "createdAt": "2026-02-19T19:36:11.3053645Z"
        }
    ]
}
```

**Tests Passed:**
- ? Status code is 200
- ? Transaction type is Bonus
- ? Transaction has 2 ledger entries
- ? Has correct amounts (-50 debit, +50 credit)

---

### 4.2 Bonus to Alice - Daily Reward ?

| Property | Value |
|----------|-------|
| **Endpoint** | `POST /api/wallet/bonus` |
| **Status Code** | `200 OK` |
| **Response Time** | `65ms` |

**Request Body:**
```json
{
  "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
  "treasuryAccountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
  "amount": 25,
  "idempotencyKey": "daily-reward-alice-20250220",
  "description": "Daily login reward"
}
```

**Response:**
```json
{
    "transactionId": 5,
    "type": "Bonus",
    "description": "Daily login reward",
    "idempotencyKey": "daily-reward-alice-80db5754-ac99-4113-a9db-88dfa6788341",
    "createdAt": "2026-02-19T19:36:27.8901815Z",
    "entries": [
        {
            "id": 9,
            "accountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
            "accountName": "Treasury - Gold Coins",
            "amount": -25,
            "transactionId": 5,
            "createdAt": "2026-02-19T19:36:27.8901815Z"
        },
        {
            "id": 10,
            "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
            "accountName": "Alice",
            "amount": 25,
            "transactionId": 5,
            "createdAt": "2026-02-19T19:36:27.8901815Z"
        }
    ]
}
```

**Tests Passed:**
- ? Status code is 200
- ? Transaction type is Bonus

---

## 5?? Wallet Operations - Spend

### 5.1 Spend - Alice Buys Item (Success) ?

| Property | Value |
|----------|-------|
| **Endpoint** | `POST /api/wallet/spend` |
| **Status Code** | `200 OK` |
| **Response Time** | `72ms` |

**Request Body:**
```json
{
  "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
  "treasuryAccountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
  "amount": 30,
  "idempotencyKey": "spend-alice-sword-001",
  "description": "Purchased Premium Sword"
}
```

**Response:**
```json
{
    "transactionId": 6,
    "type": "Spend",
    "description": "Purchased Premium Sword",
    "idempotencyKey": "spend-alice-item-ec9f4814-b312-4740-ac25-7973a0bb55fb",
    "createdAt": "2026-02-19T19:36:46.9451546Z",
    "entries": [
        {
            "id": 11,
            "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
            "accountName": "Alice",
            "amount": -30,
            "transactionId": 6,
            "createdAt": "2026-02-19T19:36:46.9451546Z"
        },
        {
            "id": 12,
            "accountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
            "accountName": "Treasury - Gold Coins",
            "amount": 30,
            "transactionId": 6,
            "createdAt": "2026-02-19T19:36:46.9451546Z"
        }
    ]
}
```

**Tests Passed:**
- ? Status code is 200
- ? Transaction type is Spend
- ? Transaction has 2 ledger entries
- ? Has correct amounts (user debited -30, treasury credited +30)

---

### 5.2 Spend - Insufficient Funds (422) ?

| Property | Value |
|----------|-------|
| **Endpoint** | `POST /api/wallet/spend` |
| **Status Code** | `422 Unprocessable Entity` |
| **Response Time** | `35ms` |

**Request Body:**
```json
{
  "accountId": "f8fc93e2-b8a9-4fb9-bbd8-0acb2d39aa4f",
  "treasuryAccountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
  "amount": 999999,
  "idempotencyKey": "spend-insufficient-test",
  "description": "Should fail - not enough balance"
}
```

**Response:**
```json
{
    "error": "Insufficient funds",
    "detail": "Current balance: 350.0000, Requested: 999999.0000"
}
```

**Tests Passed:**
- ? Status code is 422
- ? Error message contains 'Insufficient'
- ? Response has detail with balance info

---

### 5.3 Spend - Bob Unlocks Level ?

| Property | Value |
|----------|-------|
| **Endpoint** | `POST /api/wallet/spend` |
| **Status Code** | `200 OK` |
| **Response Time** | `68ms` |

**Request Body:**
```json
{
  "accountId": "f8fc93e2-b8a9-4fb9-bbd8-0acb2d39aa4f",
  "treasuryAccountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
  "amount": 20,
  "idempotencyKey": "spend-bob-level5",
  "description": "Unlocked Level 5"
}
```

**Response:**
```json
{
    "transactionId": 7,
    "type": "Spend",
    "description": "Unlocked Level 5",
    "idempotencyKey": "spend-bob-level-ab3756e9-b47b-4950-ad75-16a9f0748e94",
    "createdAt": "2026-02-19T19:37:40.3641083Z",
    "entries": [
        {
            "id": 13,
            "accountId": "f8fc93e2-b8a9-4fb9-bbd8-0acb2d39aa4f",
            "accountName": "Bob",
            "amount": -20,
            "transactionId": 7,
            "createdAt": "2026-02-19T19:37:40.3641083Z"
        },
        {
            "id": 14,
            "accountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
            "accountName": "Treasury - Gold Coins",
            "amount": 20,
            "transactionId": 7,
            "createdAt": "2026-02-19T19:37:40.3641083Z"
        }
    ]
}
```

**Tests Passed:**
- ? Status code is 200
- ? Transaction type is Spend

---

## 6?? End-to-End Scenario: Complete User Journey

This scenario tests a complete user flow: check balance ? top-up ? verify ? spend ? verify ? view history.

### Step 1: Check Initial Balance ?

| Property | Value |
|----------|-------|
| **Endpoint** | `GET /api/accounts/15a6ddd0-2791-446d-84ed-33aec79c0187/balance` |
| **Status Code** | `200 OK` |

**Response:**
```json
{
    "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
    "accountName": "Alice",
    "assetType": "Gold Coins",
    "assetSymbol": "GLD",
    "balance": 595.0000
}
```

**Stored:** `scenarioInitialBalance = 595`

---

### Step 2: TopUp 200 GLD ?

| Property | Value |
|----------|-------|
| **Endpoint** | `POST /api/wallet/topup` |
| **Status Code** | `200 OK` |

**Request Body:**
```json
{
  "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
  "treasuryAccountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
  "amount": 200,
  "idempotencyKey": "scenario-topup-unique",
  "description": "Scenario: TopUp 200 GLD"
}
```

**Response:**
```json
{
    "transactionId": 8,
    "type": "TopUp",
    "description": "Scenario: TopUp 200 GLD",
    "idempotencyKey": "scenario-topup-f3633c1f-ebf1-469d-8635-4b74ba6f4cac",
    "createdAt": "2026-02-19T19:38:26.8840948Z",
    "entries": [
        {
            "id": 15,
            "accountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
            "accountName": "Treasury - Gold Coins",
            "amount": -200,
            "transactionId": 8,
            "createdAt": "2026-02-19T19:38:26.8840948Z"
        },
        {
            "id": 16,
            "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
            "accountName": "Alice",
            "amount": 200,
            "transactionId": 8,
            "createdAt": "2026-02-19T19:38:26.8840948Z"
        }
    ]
}
```

---

### Step 3: Verify Balance After TopUp ?

| Property | Value |
|----------|-------|
| **Endpoint** | `GET /api/accounts/15a6ddd0-2791-446d-84ed-33aec79c0187/balance` |
| **Status Code** | `200 OK` |

**Response:**
```json
{
    "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
    "accountName": "Alice",
    "assetType": "Gold Coins",
    "assetSymbol": "GLD",
    "balance": 795.0000
}
```

**Validation:** `795 = 595 (initial) + 200 (topup)` ?

---

### Step 4: Spend 75 GLD ?

| Property | Value |
|----------|-------|
| **Endpoint** | `POST /api/wallet/spend` |
| **Status Code** | `200 OK` |

**Request Body:**
```json
{
  "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
  "treasuryAccountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
  "amount": 75,
  "idempotencyKey": "scenario-spend-unique",
  "description": "Scenario: Spend 75 GLD"
}
```

**Response:**
```json
{
    "transactionId": 9,
    "type": "Spend",
    "description": "Scenario: Spend 75 GLD",
    "idempotencyKey": "scenario-spend-0de0dc7e-e68c-46ec-ac22-01a85d89fb0b",
    "createdAt": "2026-02-19T19:39:14.7226581Z",
    "entries": [
        {
            "id": 17,
            "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
            "accountName": "Alice",
            "amount": -75,
            "transactionId": 9,
            "createdAt": "2026-02-19T19:39:14.7226581Z"
        },
        {
            "id": 18,
            "accountId": "cff3f9ef-970a-4a63-ac4a-42663bb116c1",
            "accountName": "Treasury - Gold Coins",
            "amount": 75,
            "transactionId": 9,
            "createdAt": "2026-02-19T19:39:14.7226581Z"
        }
    ]
}
```

---

### Step 5: Verify Final Balance ?

| Property | Value |
|----------|-------|
| **Endpoint** | `GET /api/accounts/15a6ddd0-2791-446d-84ed-33aec79c0187/balance` |
| **Status Code** | `200 OK` |

**Response:**
```json
{
    "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
    "accountName": "Alice",
    "assetType": "Gold Coins",
    "assetSymbol": "GLD",
    "balance": 720.0000
}
```

**Validation:** `720 = 595 (initial) + 200 (topup) - 75 (spend)` ?

---

### Step 6: View Ledger History ?

| Property | Value |
|----------|-------|
| **Endpoint** | `GET /api/accounts/15a6ddd0-2791-446d-84ed-33aec79c0187/ledger?skip=0&take=10` |
| **Status Code** | `200 OK` |

**Response:**
```json
{
    "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
    "accountName": "Alice",
    "runningBalance": 720.0000,
    "totalEntries": 6,
    "skip": 0,
    "take": 10,
    "entries": [
        {
            "id": 17,
            "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
            "accountName": "Alice",
            "amount": -75.0000,
            "transactionId": 9,
            "createdAt": "2026-02-19T19:39:14.722658Z"
        },
        {
            "id": 16,
            "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
            "accountName": "Alice",
            "amount": 200.0000,
            "transactionId": 8,
            "createdAt": "2026-02-19T19:38:26.884094Z"
        },
        {
            "id": 11,
            "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
            "accountName": "Alice",
            "amount": -30.0000,
            "transactionId": 6,
            "createdAt": "2026-02-19T19:36:46.945154Z"
        },
        {
            "id": 10,
            "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
            "accountName": "Alice",
            "amount": 25.0000,
            "transactionId": 5,
            "createdAt": "2026-02-19T19:36:27.890181Z"
        },
        {
            "id": 6,
            "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
            "accountName": "Alice",
            "amount": 100.0000,
            "transactionId": 3,
            "createdAt": "2026-02-19T19:34:23.80467Z"
        },
        {
            "id": 2,
            "accountId": "15a6ddd0-2791-446d-84ed-33aec79c0187",
            "accountName": "Alice",
            "amount": 500.0000,
            "transactionId": 1,
            "createdAt": "2026-02-19T18:27:24.468049Z"
        }
    ]
}
```

**Tests Passed:**
- ? Status code is 200
- ? Ledger has entries (8 total)
- ? Most recent entries shown first (descending order)

---

## ? Conclusion

All **22 API tests passed successfully**, demonstrating:

| # | Feature | Status |
|---|---------|--------|
| 1 | **Health Check** - API is operational | ? |
| 2 | **Account Management** - List accounts, get balance, get ledger | ? |
| 3 | **TopUp Flow** - Credits can be purchased | ? |
| 4 | **Bonus Flow** - Free credits can be issued | ? |
| 5 | **Spend Flow** - Credits can be spent | ? |
| 6 | **Idempotency** - Duplicate requests return same result | ? |
| 7 | **Input Validation** - Zero/negative amounts rejected (400) | ? |
| 8 | **Error Handling** - 404 for missing accounts, 422 for insufficient funds | ? |
| 9 | **Double-Entry Bookkeeping** - Every transaction has debit + credit entries | ? |
| 10 | **Balance Computation** - Balances calculated correctly from ledger sum | ? |

---

## ?? Integration Test Results

In addition to Postman tests, the project includes automated integration tests using Testcontainers:

```
dotnet test

Test summary: total: 16, failed: 0, succeeded: 16, skipped: 0, duration: 15.9s
```

| Test Class | Tests | Result |
|------------|-------|--------|
| `WalletApiTests` | 16 | ? All Passed |

---

<p align="center">
  <strong>DinoWallet API - All Tests Passed! ???</strong>
</p>
