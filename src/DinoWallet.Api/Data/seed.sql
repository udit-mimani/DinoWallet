-- =============================================================================
-- Dino Ventures — Wallet Service Seed Data
-- Run after migrations: psql -d dinowallet -f seed.sql
-- =============================================================================

-- Ensure idempotent: re-running won't duplicate data
BEGIN;

-- ── Asset Types ───────────────────────────────────────────────────────────────
INSERT INTO "AssetTypes" ("Name", "Symbol", "IsActive")
VALUES
  ('Gold Coins',    'GLD', true),
  ('Diamonds',      'DIA', true),
  ('Loyalty Points','LPT', true)
ON CONFLICT ("Symbol") DO NOTHING;

-- ── System (Treasury) Accounts ────────────────────────────────────────────────
INSERT INTO "Accounts" ("Id", "Name", "Type", "AssetTypeId", "CreatedAt")
SELECT
  gen_random_uuid(),
  'Treasury - ' || at."Name",
  1,            -- AccountType.System
  at."Id",
  now()
FROM "AssetTypes" at
WHERE NOT EXISTS (
  SELECT 1 FROM "Accounts" a
  WHERE a."Name" = 'Treasury - ' || at."Name"
);

-- ── User Accounts ─────────────────────────────────────────────────────────────
-- Two users, each with a Gold Coins wallet
INSERT INTO "Accounts" ("Id", "Name", "Type", "AssetTypeId", "CreatedAt")
SELECT gen_random_uuid(), u.name, 0, at."Id", now()
FROM (VALUES ('Alice'), ('Bob')) AS u(name)
CROSS JOIN "AssetTypes" at
WHERE at."Symbol" = 'GLD'
AND NOT EXISTS (
  SELECT 1 FROM "Accounts" a WHERE a."Name" = u.name AND a."AssetTypeId" = at."Id"
);

-- ── Seed Balances via Double-Entry Ledger ─────────────────────────────────────
-- Give Alice 500 GLD and Bob 300 GLD from Treasury.
-- Each balance grant is modelled as a proper transaction with two ledger lines.

DO $$
DECLARE
  v_treasury_id   UUID;
  v_alice_id      UUID;
  v_bob_id        UUID;
  v_txn_id        BIGINT;
  v_asset_id      INT;
BEGIN
  SELECT "Id" INTO v_asset_id FROM "AssetTypes" WHERE "Symbol" = 'GLD';
  SELECT "Id" INTO v_treasury_id FROM "Accounts" WHERE "Name" = 'Treasury - Gold Coins';
  SELECT "Id" INTO v_alice_id    FROM "Accounts" WHERE "Name" = 'Alice' AND "AssetTypeId" = v_asset_id;
  SELECT "Id" INTO v_bob_id      FROM "Accounts" WHERE "Name" = 'Bob'   AND "AssetTypeId" = v_asset_id;

  -- ── Alice: initial balance of 500 GLD ──────────────────────────────────────
  IF NOT EXISTS (SELECT 1 FROM "Transactions" WHERE "IdempotencyKey" = 'seed-alice-initial') THEN
    INSERT INTO "Transactions" ("Type", "Description", "IdempotencyKey", "CreatedAt")
    VALUES (0, 'Initial seed balance for Alice', 'seed-alice-initial', now())
    RETURNING "Id" INTO v_txn_id;

    INSERT INTO "LedgerEntries" ("AccountId", "TransactionId", "Amount", "CreatedAt") VALUES
      (v_treasury_id, v_txn_id, -500.0000, now()),  -- debit treasury
      (v_alice_id,    v_txn_id,  500.0000, now());  -- credit Alice

    INSERT INTO "IdempotencyRecords" ("Key", "TransactionId", "CreatedAt")
    VALUES ('seed-alice-initial', v_txn_id, now());
  END IF;

  -- ── Bob: initial balance of 300 GLD ────────────────────────────────────────
  IF NOT EXISTS (SELECT 1 FROM "Transactions" WHERE "IdempotencyKey" = 'seed-bob-initial') THEN
    INSERT INTO "Transactions" ("Type", "Description", "IdempotencyKey", "CreatedAt")
    VALUES (0, 'Initial seed balance for Bob', 'seed-bob-initial', now())
    RETURNING "Id" INTO v_txn_id;

    INSERT INTO "LedgerEntries" ("AccountId", "TransactionId", "Amount", "CreatedAt") VALUES
      (v_treasury_id, v_txn_id, -300.0000, now()),  -- debit treasury
      (v_bob_id,      v_txn_id,  300.0000, now());  -- credit Bob

    INSERT INTO "IdempotencyRecords" ("Key", "TransactionId", "CreatedAt")
    VALUES ('seed-bob-initial', v_txn_id, now());
  END IF;

END $$;

COMMIT;

-- ── Verification Queries ──────────────────────────────────────────────────────
-- Run these to confirm seed worked:
--
-- SELECT a."Name", SUM(le."Amount") AS balance
-- FROM "Accounts" a
-- JOIN "LedgerEntries" le ON le."AccountId" = a."Id"
-- GROUP BY a."Name"
-- ORDER BY a."Name";
--
-- Expected:
--   Alice           |  500.0000
--   Bob             |  300.0000
--   Treasury - GLD  | -800.0000
