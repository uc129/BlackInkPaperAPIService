---                                                                                                                                                           
Phase 1 — Security Hardening (~1 day, ship first)

The gaps that are actively dangerous before any real traffic:

1. CORS lockdown — move allowed origins to appsettings.json, replace AllowAnyOrigin()
2. Secrets out of config — DB password, JWT key, Cloudinary ApiSecret, Razorpay keys → .NET user-secrets locally, App Service env vars / Key Vault in         
   production
3. DTO validation — [Required], [EmailAddress], [Range] on RegisterRequest, LoginRequest, AddCartItemRequest, and the variant/product request DTOs; wire up
   InvalidModelStateResponseFactory to return ServiceResponse-shaped 400s
4. Rate limiting — auth policy (5/min/IP on login+register), storage policy (10/min/user on signing), global fallback (120/min/IP); 3 lines in Program.cs +
   [EnableRateLimiting] on the relevant controllers
5. StorageController parameter whitelist — reject any key not in { "folder", "tags", "context", "resource_type" }

  ---                                                                                                                                                           
Phase 2 — API Infrastructure (~1 day, parallel to Phase 1)

1. Global exception middleware — replaces UseExceptionHandler("/Home/Error") (which returns HTML); new GlobalExceptionMiddleware.cs returns ProblemDetails  
   JSON with a correlation ID always
2. Swagger — Swashbuckle.AspNetCore, dev-only, with Bearer auth definition; fill in the missing [ProducesResponseType] on AdminOrdersController and
   CartController
3. Health check — AddHealthChecks().AddNpgSql(...) + MapHealthChecks("/health") — one line each, needed by Azure's health probe
4. Response compression — Brotli + Gzip via AddResponseCompression()
5. HTTP request logging — ASP.NET Core's built-in AddHttpLogging for method/path/status/duration (feeds Azure Monitor with zero extra code)

  ---                                                                                                                                                           
Phase 3 — Missing User-Facing Endpoints (~2 days)

1. IEmailService + SendGrid implementation — unblocks everything below
2. GET/PATCH /api/account/profile — users currently have no way to view or update their own data
3. POST /api/account/change-password
4. POST /api/account/forgot-password + reset-password — no enumeration (always 200)
5. DELETE /api/storage/{publicId} — add DeleteAsync to IStorageService, implement in CloudinaryStorageService, expose endpoint; currently uploaded images are
   permanent and unremovable
6. POST /api/checkout/orders/{id}/cancel — only if status is Pending/Confirmed

  ---                                                                                                                                                         
Phase 4 — Pagination & Data Quality (~1 day, self-contained)

1. GET /api/checkout/orders — currently returns unbounded IReadOnlyList<OrderDto>; add page/pageSize query params, return PagedResultDto<OrderDto>
2. MaxPageSize guard on ProductSearchRequest, AdminOrderSearchRequest, AdminUserSearchRequest — clamp to 100
3. Reference data cap — add LIMIT 500 to artists/categories/tags queries (sufficient for dropdowns, prevents full-table scans as data grows)
4. Shipping addresses cap — hard limit of 20 per user

  ---                                                                                                                                                           
Phase 5 — Production Hardening (~2 days, requires Phase 3)

1. Admin audit log middleware — intercepts all POST/PUT/PATCH/DELETE to /api/admin/..., writes { userId, method, path, statusCode, timestamp } to a new
   AuditLogs table
2. API versioning — Asp.Versioning.Mvc, URL segment strategy (/api/v1/...), AssumeDefaultVersionWhenUnspecified = true keeps existing clients working
3. Refresh tokens — new RefreshTokens table, POST /api/account/refresh endpoint, login response includes { accessToken, refreshToken, expiresIn }; token      
   rotation on each refresh
4. Email verification — on register, send confirmation link; add emailVerified flag to profile response; gate order placement behind it

  ---                                                                                                                                                         
Summary

┌───────┬───────────────────┬─────────┬─────────────────────────────┐
│ Phase │        Theme         │ Effort  │         Dependency          │                                                                                      
├───────┼──────────────────────┼─────────┼─────────────────────────────┤                                                                                    
│ 1     │ Security             │ ~1 day  │ —                           │
├───────┼──────────────────────┼─────────┼─────────────────────────────┤
│ 2     │ Infrastructure       │ ~1 day  │ — (parallel to 1)           │
├───────┼──────────────────────┼─────────┼─────────────────────────────┤                                                                                      
│ 3     │ Missing endpoints    │ ~2 days │ Phase 1 validation patterns │
├───────┼──────────────────────┼─────────┼─────────────────────────────┤                                                                                      
│ 4     │ Pagination           │ ~1 day  │ — (self-contained)          │                                                                                    
├───────┼──────────────────────┼─────────┼─────────────────────────────┤
│ 5     │ Production hardening │ ~2 days │ Phase 3 email service       │
└───────┴──────────────────────┴─────────┴─────────────────────────────┘       