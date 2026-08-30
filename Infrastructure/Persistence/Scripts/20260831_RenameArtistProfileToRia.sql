-- The artist profile predates the handover and still carries the placeholder identity.
-- SeedRiaArtwork.sql refuses to run until this is corrected, so that the catalogue can
-- never be credited to the wrong person.
--
-- Idempotent: renames only a profile that is not already Ria's, and clears the invented
-- social/portfolio URLs rather than substituting made-up ones for a real person.

UPDATE ArtistProfiles
SET DisplayName     = 'Ria Mukharjee',
    Bio             = 'Illustrator working in ink and fine detail. Studied in Dubai and New Delhi.',
    ProfileImageUrl = NULL,
    CoverImageUrl   = NULL,
    InstagramUrl    = NULL,
    PortfolioUrl    = NULL,
    WebsiteUrl      = NULL,
    TotalSales      = 0,
    UpdatedAt       = CURRENT_TIMESTAMP
WHERE UserId = 'user-artist'
  AND DisplayName <> 'Ria Mukharjee';

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM ArtistProfiles WHERE DisplayName = 'Ria Mukharjee') THEN
        RAISE EXCEPTION
            'No ArtistProfiles row was renamed. Check the UserId - expected ''user-artist''.';
    END IF;
END $$;
