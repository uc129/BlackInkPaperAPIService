-- Audit trail for admin write operations
CREATE TABLE IF NOT EXISTS AuditLogs (
    Id          BIGSERIAL    PRIMARY KEY,
    UserId      TEXT         NOT NULL,
    UserEmail   TEXT,
    Method      TEXT         NOT NULL,
    Path        TEXT         NOT NULL,
    StatusCode  INT,
    IpAddress   TEXT,
    OccurredAt  TIMESTAMPTZ  NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_auditlogs_userid      ON AuditLogs (UserId);
CREATE INDEX IF NOT EXISTS ix_auditlogs_occurredat  ON AuditLogs (OccurredAt DESC);

-- Refresh tokens (short-lived, single-use, stored as SHA-256 hashes)
CREATE TABLE IF NOT EXISTS RefreshTokens (
    Id          BIGSERIAL    PRIMARY KEY,
    UserId      TEXT         NOT NULL,
    TokenHash   TEXT         NOT NULL UNIQUE,
    ExpiresAt   TIMESTAMPTZ  NOT NULL,
    CreatedAt   TIMESTAMPTZ  NOT NULL,
    RevokedAt   TIMESTAMPTZ
);
CREATE INDEX IF NOT EXISTS ix_refreshtokens_tokenhash ON RefreshTokens (TokenHash);
CREATE INDEX IF NOT EXISTS ix_refreshtokens_userid    ON RefreshTokens (UserId);
