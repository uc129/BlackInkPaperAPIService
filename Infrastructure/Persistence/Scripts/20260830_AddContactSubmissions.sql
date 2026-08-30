-- Contact form submissions (public POST /api/contact, triaged via /api/admin/contact)
--
-- Columns are declared in snake_case rather than the CamelCase used by the older tables
-- because ContactRepository queries them that way; Dapper bridges the two via
-- DefaultTypeMap.MatchNamesWithUnderscores (set in Program.cs).
--
-- Lengths mirror the validation attributes on SubmitContactRequest.
CREATE TABLE IF NOT EXISTS contact_submissions (
    id              SERIAL         PRIMARY KEY,
    name            VARCHAR(200)   NOT NULL,
    email           VARCHAR(320)   NOT NULL,
    subject         VARCHAR(500)   NOT NULL,
    message         VARCHAR(5000)  NOT NULL,
    submitted_at    TIMESTAMPTZ    NOT NULL,
    is_resolved     BOOLEAN        NOT NULL DEFAULT FALSE,
    resolved_at     TIMESTAMPTZ,
    resolved_notes  TEXT
);

-- Admin inbox orders by submitted_at DESC and filters on is_resolved.
CREATE INDEX IF NOT EXISTS ix_contact_submissions_submitted_at ON contact_submissions (submitted_at DESC);
CREATE INDEX IF NOT EXISTS ix_contact_submissions_is_resolved  ON contact_submissions (is_resolved);
