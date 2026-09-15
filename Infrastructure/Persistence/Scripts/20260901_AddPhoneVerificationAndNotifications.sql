-- Phone-first authentication and outbound customer notifications.
--
-- snake_case columns, as in 20260830_AddContactSubmissions.sql: the repositories query them
-- that way and Dapper bridges to the C# PascalCase via DefaultTypeMap.MatchNamesWithUnderscores
-- (set in Program.cs). The older CamelCase tables predate that convention.

-- ── OTP challenges ───────────────────────────────────────────────────────────
-- One row per OTP send. Codes are stored as an HMAC keyed on a server-side secret and
-- salted with the phone number, never in plaintext: a 6-digit code has only a million
-- possible values, so a plain digest would be trivially reversible if the table leaked.
--
-- Rows are kept after use rather than deleted — consumed_at and attempts are what the
-- per-phone send throttle and the abuse audit read.
CREATE TABLE IF NOT EXISTS phone_verification_codes (
    id            BIGSERIAL    PRIMARY KEY,
    phone_e164    VARCHAR(20)  NOT NULL,
    code_hash     TEXT         NOT NULL,
    purpose       VARCHAR(32)  NOT NULL,
    attempts      INT          NOT NULL DEFAULT 0,
    max_attempts  INT          NOT NULL DEFAULT 5,
    expires_at    TIMESTAMPTZ  NOT NULL,
    consumed_at   TIMESTAMPTZ,
    created_at    TIMESTAMPTZ  NOT NULL,
    request_ip    VARCHAR(64),
    channel       VARCHAR(16)
);

-- Verify looks up the newest live challenge for a number; the throttle counts recent sends
-- for that number. Both are (phone, created_at DESC).
CREATE INDEX IF NOT EXISTS ix_phone_verification_codes_phone_created
    ON phone_verification_codes (phone_e164, created_at DESC);

-- ── Notification outbox ──────────────────────────────────────────────────────
-- Order notifications are enqueued here rather than sent inline, so a slow or failing
-- provider cannot delay checkout and a transient outage cannot silently lose a message.
--
-- dedupe_key is the load-bearing column. A captured payment reaches us twice in the normal
-- case — the client calls verify-payment AND Razorpay posts its webhook, both landing in
-- MarkPaymentCapturedAndApplyInventory. Enqueue is ON CONFLICT DO NOTHING against this
-- unique constraint, so the second arrival is a no-op instead of a duplicate message.
CREATE TABLE IF NOT EXISTS notification_deliveries (
    id                   BIGSERIAL    PRIMARY KEY,
    dedupe_key           TEXT         NOT NULL UNIQUE,
    user_id              TEXT,
    recipient            VARCHAR(20)  NOT NULL,
    channel              VARCHAR(16)  NOT NULL,
    template_name        VARCHAR(64)  NOT NULL,
    payload              JSONB        NOT NULL,
    status               VARCHAR(16)  NOT NULL DEFAULT 'Pending',
    attempts             INT          NOT NULL DEFAULT 0,
    max_attempts         INT          NOT NULL DEFAULT 5,
    last_error           TEXT,
    scheduled_for        TIMESTAMPTZ  NOT NULL,
    sent_at              TIMESTAMPTZ,
    provider_message_id  TEXT,
    created_at           TIMESTAMPTZ  NOT NULL,
    updated_at           TIMESTAMPTZ  NOT NULL
);

-- The dispatcher polls for due work. It must also see rows stuck in 'Sending' — a process
-- that dies or is redeployed mid-batch leaves them there, and if the query ignored them they
-- would never be retried by anyone. Hence both statuses in the filter.
CREATE INDEX IF NOT EXISTS ix_notification_deliveries_due
    ON notification_deliveries (status, scheduled_for)
    WHERE status IN ('Pending', 'Sending');

CREATE INDEX IF NOT EXISTS ix_notification_deliveries_user
    ON notification_deliveries (user_id);
