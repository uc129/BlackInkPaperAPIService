using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence
{
    public class AppIdentityUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? ArtistPortfolioUrl { get; set; }

        /// <summary>
        /// When the customer agreed to receive order updates over WhatsApp, or null if they
        /// have not. A timestamp rather than a bool because Meta requires us to be able to
        /// show when consent was given, not merely that it was.
        /// </summary>
        public DateTime? WhatsAppOptInAt { get; set; }

        /// <summary>
        /// Consent for marketing sends (abandoned cart, new artwork drops) — a separate and
        /// stricter permission than <see cref="WhatsAppOptInAt"/>, which covers only
        /// transactional order updates. Cleared when the customer opts out.
        /// </summary>
        public DateTime? MarketingOptInAt { get; set; }
    }

    public class AppIdentityDbContext : IdentityDbContext<AppIdentityUser>
    {
        public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // You can rename Identity tables here if you prefer (e.g. Users, Roles)
            builder.Entity<AppIdentityUser>().ToTable("Users");
            builder.Entity<IdentityRole>().ToTable("Roles");

            // Identity's own EmailIndex is non-unique; uniqueness normally comes from
            // User.RequireUniqueEmail, which we cannot enable because it rejects the blank
            // email of a phone-only account. This filtered index restores the guarantee for
            // the rows that actually have an email, and leaves phone-only users alone.
            builder.Entity<AppIdentityUser>()
                .HasIndex(u => u.NormalizedEmail)
                .HasDatabaseName("ix_users_normalizedemail_unique")
                .IsUnique()
                .HasFilter("\"NormalizedEmail\" IS NOT NULL");

            // PhoneNumber is the authoritative phone identity — it is what phone login looks
            // up, and it is set both when an account is created from a number and when an
            // existing email account links one. Identity gives it no uniqueness of its own, so
            // without this two accounts could claim the same number and a customer logging in
            // by phone could land in either.
            builder.Entity<AppIdentityUser>()
                .HasIndex(u => u.PhoneNumber)
                .HasDatabaseName("ix_users_phonenumber_unique")
                .IsUnique()
                .HasFilter("\"PhoneNumber\" IS NOT NULL");
        }
    }
}
