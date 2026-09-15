# Production Services — what to subscribe to

Every third-party account the API needs, what breaks without it, and the exact configuration key
each one feeds. Derived from `Program.cs` and `Infrastructure/Configuration/`, not from memory.

**Secrets never go in `appsettings.json`.** In Azure App Service each one is an Application Setting;
the name is the config path with `:` replaced by `__` (e.g. `Razorpay__KeySecret`). That is the
same convention `Msg91__AuthKey` and `SendGrid__ApiKey` already use.

---

## Priority 1 — the store cannot trade without these

### Razorpay — payments
Indian payment gateway. Handles card/UPI/netbanking and the capture webhook.

| Setting | Env var | Notes |
|---|---|---|
| `Razorpay:KeyId` | `Razorpay__KeyId` | Public; also returned to the browser in `PaymentSessionDto` |
| `Razorpay:KeySecret` | `Razorpay__KeySecret` | Signs payment verification |
| `Razorpay:WebhookSecret` | `Razorpay__WebhookSecret` | Separate secret, set when you create the webhook |
| `Razorpay:DisplayName` / `DisplayDescription` | — | Shown in the Checkout modal; safe in appsettings |

**Signup:** business KYC — PAN, GST if registered, bank account, and a website that shows
pricing, refund/cancellation and shipping policies. Allow several days; live keys are not issued
until KYC clears. Test keys work immediately for integration.

**Webhook to register in the Razorpay dashboard:**
```
POST https://<your-api-host>/api/payments/razorpay/webhook
```
Subscribe at minimum to `payment.captured` and `payment.failed`.

> The path in `CLAUDE.md` (`/api/razorpay/webhook`) is **wrong** — the real route is
> `/api/payments/razorpay/webhook` (`RazorpayWebhookController`). Registering the wrong one means
> captures silently never reconcile.

**No fallback.** Without keys, `payment-session` fails and nothing can be bought.

---

### MSG91 — phone login OTP and order notifications
Sends the login code over WhatsApp with SMS fallback, and all order-status messages.

| Setting | Env var | Notes |
|---|---|---|
| `Msg91:AuthKey` | `Msg91__AuthKey` | **The switch.** Unset ⇒ stub senders (see warning below) |
| `Msg91:WhatsAppNumber` | `Msg91__WhatsAppNumber` | Your approved WhatsApp sender |
| `Msg91:SmsSenderId` | `Msg91__SmsSenderId` | 6-char DLT-registered header |
| `Msg91:Templates:OtpWhatsApp` | — | Default `otp_login` |
| `Msg91:Templates:OtpSms` | — | **Blank in appsettings — must be set to your DLT template id** |
| `Msg91:Templates:Order*` / `PaymentFailed` | — | `order_confirmed`, `order_shipped`, `order_delivered`, `order_cancelled`, `payment_failed` |

> ⚠️ **If `Msg91:AuthKey` is unset, `Program.cs:158-174` registers `LoggingWhatsAppSender` /
> `LoggingSmsSender`, which write the OTP to the application log instead of sending it.** That is
> correct for local development and a credential-disclosure bug in production. Setting the auth
> key is what prevents it — there is no separate production guard.

**Signup, and the part with a lead time:**
1. **MSG91 account** — immediate.
2. **DLT registration (TRAI)** — mandatory for *any* SMS to Indian numbers. Register the entity,
   the sender ID, and each template on a DLT portal (Jio/Airtel/VI). Typically several days.
   Until it completes the SMS fallback silently fails.
3. **WhatsApp Business via Meta** — verified business, an approved sender number, and each
   template approved individually. `otp_login` must be an *authentication*-category template;
   the order templates are *utility*. Also days, sometimes longer.

Start both (2) and (3) early — they gate launch more than the code does.

**Partial degradation:** WhatsApp is tried first, SMS is the fallback (`OtpDeliveryService`).
With WhatsApp approved but DLT pending, login works for customers who have WhatsApp and fails
for everyone else.

---

### PostgreSQL — Supabase
Primary datastore for everything except Identity migrations, which are EF Core on the same DB.

| Setting | Env var |
|---|---|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` |

Use the **pooler** host for a web app, not the direct connection. A free-tier project pauses when
idle, which presents as intermittent 500s — use a paid tier for anything customer-facing.

`ConnectionStrings:DefaultConnectionSQLSERVER` is a legacy Azure SQL fallback and is not used by
the running app. Retire it rather than maintaining a second database.

---

### Cloudinary — artwork image hosting
Hosts every catalogue image, and signs upload requests from the admin portal.

Two distinct needs, worth separating:
- **Delivery** — the storefront loads images straight from Cloudinary CDN URLs baked into the
  seed data. This needs the *account* to exist and hold the assets, but no API credentials.
- **Uploads** — `CloudinaryStorageService.GenerateUploadSignature` needs all three keys. Adding
  new artwork through the admin portal fails without them.

| Setting | Env var |
|---|---|
| `Cloudinary:CloudName` | `Cloudinary__CloudName` |
| `Cloudinary:ApiKey` | `Cloudinary__ApiKey` |
| `Cloudinary:ApiSecret` | `Cloudinary__ApiSecret` |

Free tier is generous and realistically sufficient at launch volume. **No fallback** —
`CloudinaryStorageService` is registered unconditionally (`Program.cs:122`), but it is scoped, so
missing credentials do not stop the app booting; they surface when a storage operation runs.

---

## Priority 2 — degrades gracefully, but you want it

### SendGrid — transactional email
Email confirmation, password reset, and the contact-form notification.

| Setting | Env var |
|---|---|
| `SendGrid:ApiKey` | `SendGrid__ApiKey` |
| `SendGrid:FromEmail` / `FromName` | — |

**Fallback:** unset ⇒ `StubEmailService`, which logs instead of sending. The app runs, but nobody
can confirm an email or reset a password — and an email-only account that never confirms is
blocked from checkout by the `contact_not_verified` gate.

**Signup:** free tier ~100 emails/day. Requires **domain authentication** (SPF/DKIM DNS records)
or your mail lands in spam. Single-sender verification works for testing only.

Also set `Contact:AdminNotificationEmail` — where contact-form submissions are sent.

---

## Priority 3 — infrastructure you already have

| Service | Purpose | Notes |
|---|---|---|
| **Azure App Service** | Hosts two web apps: `BlackInkPaperAPIService` and `BlackInkPaper-Admin` | Where every secret above lives, as Application Settings |
| **GitHub Actions** | CI/CD on push to `main` | Needs `AZUREAPPSERVICE_PUBLISHPROFILE_*` secrets (both already configured) |

> `CLAUDE.md` says CI deploys the API only. The workflow actually deploys **both** API and Admin
> (`deploy-api` and `deploy-admin` jobs). Worth reconciling — the admin portal is being published
> on every push to `main`.

---

## Application secrets (not subscriptions, but required)

| Setting | Env var | Notes |
|---|---|---|
| `Jwt:Key` | `Jwt__Key` | **Rotate before launch** — the current value is committed to git |
| `Otp:HashKey` | `Otp__HashKey` | HMAC for OTP hashes. Falls back to `Jwt:Key` if unset; set it separately in production |

`Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` and the whole `Otp` tuning block
(`CodeLength` 6, `ExpiryMinutes` 10, `MaxVerifyAttempts` 5, `MaxSendsPerWindow` 3 per 10 min) are
not secret and can stay in appsettings.

---

## Storefront address

Not a subscription, but account emails are broken without it.

| Setting | Env var | Value |
|---|---|---|
| `Frontend:BaseUrl` | `Frontend__BaseUrl` | `https://blackinkpaper-store.vercel.app` (dev: `http://localhost:3000`) |

`AccountLinkBuilder` uses it to build the absolute links in the password-reset and email-confirmation
emails. Unset, it falls back to a relative link — which is useless in an email but does not throw,
so registration still completes.

The storefront must host two pages that read `email` and `token` from the query string:

| Link the email points at | Posts to |
|---|---|
| `/reset-password?email=…&token=…` | `POST /api/accounts/reset-password` |
| `/confirm-email?email=…&token=…` | `POST /api/accounts/confirm-email` |

The same origin is also in `Cors:AllowedOrigins` — without that the storefront cannot call the API
at all, regardless of email links.

---

## Launch checklist

- [ ] Razorpay KYC submitted *(days — start first)*
- [ ] DLT entity + sender ID + SMS templates registered *(days — start first)*
- [ ] Meta WhatsApp business verified, `otp_login` + 5 order templates approved *(days)*
- [ ] Supabase on a non-pausing tier, pooler connection string in App Settings
- [ ] Cloudinary account, credentials in App Settings
- [ ] SendGrid domain authentication DNS records published
- [ ] `Frontend__BaseUrl` set, and the storefront hosts `/reset-password` and `/confirm-email`
- [ ] Storefront origin present in `Cors__AllowedOrigins`
- [ ] `Msg91__AuthKey` set in production — **verify the stub sender is not active**
- [ ] `Jwt__Key` rotated, `Otp__HashKey` set independently
- [ ] Razorpay webhook registered at `/api/payments/razorpay/webhook`
- [ ] Every secret removed from `appsettings.json`; git history scrubbed or the exposed
      credentials rotated (Supabase password `C2RPqdqOhmDWNyYP` is in commit `02785d7`)
