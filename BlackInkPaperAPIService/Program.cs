using Asp.Versioning;
using BlackInkPaperAPIService.BackgroundServices;
using BlackInkPaperAPIService.Middleware;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Contracts.Services;
using Infrastructure.Configuration;
using Infrastructure.Services.Email;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Seeding;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using BlackInkPaperAPIService.Swagger;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── CORS ─────────────────────────────────────────────────────────────────────
// Override Cors:AllowedOrigins in production via environment variable:
//   CORS__ALLOWEDORIGINS=https://admin.yourdomain.com,https://yourdomain.com
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAdminUI", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
        else
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IDapperContext, DapperContext>();
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseNpgsql(connectionString));

// ── Identity ──────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<AppIdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit      = true;
    options.Password.RequiredLength    = 8;

    // Must stay false: Identity's UserValidator treats a blank email as InvalidEmail when
    // this is on, which would reject every phone-only account at CreateAsync. Email
    // uniqueness is instead enforced by a unique partial index on NormalizedEmail (see the
    // AddUniqueEmailIndex migration) plus an explicit check in the register/link paths.
    options.User.RequireUniqueEmail    = false;
})
.AddEntityFrameworkStores<AppIdentityDbContext>()
.AddDefaultTokenProviders();

// ── API Versioning ────────────────────────────────────────────────────────────
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = ctx =>
    {
        var errors = ctx.ModelState
            .Where(kv => kv.Value?.Errors.Count > 0)
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        var problem = new ValidationProblemDetails(ctx.ModelState)
        {
            Title  = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
        };
        problem.Extensions["correlationId"] = ctx.HttpContext.TraceIdentifier;

        return new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
    };
});

builder.Services.Configure<CheckoutPricingOptions>(builder.Configuration.GetSection("CheckoutPricing"));
builder.Services.Configure<RazorpayOptions>(builder.Configuration.GetSection("Razorpay"));
builder.Services.Configure<CloudinaryOptions>(builder.Configuration.GetSection(CloudinaryOptions.SectionName));
builder.Services.Configure<ContactOptions>(builder.Configuration.GetSection(ContactOptions.SectionName));

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ITokenBlackListRepo, TokenBlackListRepo>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IShippingAddressRepository, ShippingAddressRepository>();
builder.Services.AddScoped<ICartApplicationService, CartApplicationService>();
builder.Services.AddScoped<ICheckoutApplicationService, CheckoutApplicationService>();
builder.Services.AddScoped<ICheckoutPricingService, CheckoutPricingService>();
builder.Services.AddScoped<IProductApplicationService, ProductApplicationService>();
builder.Services.AddScoped<IProductReferenceDataService, ProductReferenceDataService>();
builder.Services.AddScoped<IAdminOrderService, AdminOrderService>();
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<IContactApplicationService, ContactApplicationService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IStorageService, CloudinaryStorageService>();

// Storefront address, used only to build the links in account emails.
builder.Services.Configure<FrontendOptions>(builder.Configuration.GetSection(FrontendOptions.SectionName));
builder.Services.AddScoped<AccountLinkBuilder>();

builder.Services.Configure<SendGridOptions>(builder.Configuration.GetSection("SendGrid"));
// Supplied out-of-band, never from appsettings: environment variable SendGrid__ApiKey
// (an Azure App Setting in production). Falls back to StubEmailService when unset, so
// local development works without a key and without sending real mail.
var sendGridApiKey = builder.Configuration["SendGrid:ApiKey"];
if (!string.IsNullOrWhiteSpace(sendGridApiKey))
    builder.Services.AddScoped<IEmailService, SendGridEmailService>();
else
    builder.Services.AddScoped<IEmailService, StubEmailService>();
// ── Phone auth and messaging ──────────────────────────────────────────────────
builder.Services.Configure<OtpOptions>(builder.Configuration.GetSection(OtpOptions.SectionName));
builder.Services.PostConfigure<OtpOptions>(o =>
{
    // The OTP HMAC wants its own secret (Otp__HashKey), but falling back to the JWT key keeps
    // local development working with no extra configuration. Both are secrets of the same
    // grade, and a leaked OTP hash is worth little — the codes expire in minutes.
    if (string.IsNullOrWhiteSpace(o.HashKey))
        o.HashKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Neither Otp:HashKey nor Jwt:Key is configured.");
});

builder.Services.Configure<Msg91Options>(builder.Configuration.GetSection(Msg91Options.SectionName));
builder.Services.AddScoped<IOtpCodeHasher, OtpCodeHasher>();
builder.Services.AddScoped<IPhoneVerificationRepository, PhoneVerificationRepository>();
builder.Services.AddScoped<IOtpDeliveryService, OtpDeliveryService>();
builder.Services.AddScoped<IPhoneAuthService, PhoneAuthService>();
builder.Services.AddScoped<INotificationOutboxRepository, NotificationOutboxRepository>();
builder.Services.AddScoped<IOrderNotificationService, OrderNotificationService>();
builder.Services.AddHostedService<NotificationDispatcher>();

// Supplied out-of-band, never from appsettings: environment variable Msg91__AuthKey.
// Falls back to the logging senders when unset, so local development runs the whole phone
// login flow without a vendor account — the code is read from the console instead.
var msg91AuthKey = builder.Configuration["Msg91:AuthKey"];
if (!string.IsNullOrWhiteSpace(msg91AuthKey))
{
    builder.Services.AddHttpClient<IWhatsAppSender, Msg91WhatsAppSender>(ConfigureMsg91Client);
    builder.Services.AddHttpClient<ISmsSender, Msg91SmsSender>(ConfigureMsg91Client);

    static void ConfigureMsg91Client(IServiceProvider serviceProvider, HttpClient client)
    {
        var options = serviceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<Msg91Options>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(15);
    }
}
else
{
    builder.Services.AddScoped<IWhatsAppSender, LoggingWhatsAppSender>();
    builder.Services.AddScoped<ISmsSender, LoggingSmsSender>();
}

builder.Services.AddScoped<ISeedRunner, SeedRunner>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddHttpClient<IRazorpayGateway, RazorpayGateway>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<RazorpayOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});

// ── Authentication / JWT ──────────────────────────────────────────────────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = builder.Configuration["Jwt:Issuer"],
        ValidAudience            = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    // auth endpoints: 5 requests per minute per IP (brute-force protection).
    // AddFixedWindowLimiter(name, ...) would create ONE limiter shared by every caller —
    // 5 requests/minute for the whole API rather than per client. AddPolicy with an
    // explicit partition key is what actually gives per-IP limiting.
    options.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit          = 5,
            Window               = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit           = 0,
        }));

    // OTP sends: a coarse per-IP ceiling only. The limit that actually protects the SMS bill
    // is per phone number, and it cannot live here — the partition key is resolved before the
    // request body is read, so the number is not known yet. PhoneVerificationService enforces
    // it against phone_verification_codes instead, which also survives restarts and holds
    // across multiple instances.
    options.AddPolicy("otp", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit          = 10,
            Window               = TimeSpan.FromMinutes(10),
            QueueLimit           = 0,
        }));

    // storage signing: 10 requests per minute per IP
    options.AddPolicy("storage", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window      = TimeSpan.FromMinutes(1),
            QueueLimit  = 0,
        }));

    // global fallback: 120 requests per minute per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window      = TimeSpan.FromMinutes(1),
                QueueLimit  = 0,
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Black Ink Paper API",
        Version     = "v1",
        Description = "Admin + storefront API for Black Ink Paper"
    });

    // Every response goes through ToApiResult, which wraps success in ServiceResponse<T> and
    // returns ProblemDetails on failure. The controller annotations name only the payload, so
    // without this filter the document describes `data` instead of the body.
    c.OperationFilter<ServiceResponseEnvelopeFilter>();

    // Controller and DTO <summary> docs. Missing files are skipped rather than thrown on, so a
    // project that has not enabled GenerateDocumentationFile cannot break swagger generation.
    foreach (var xml in Directory.GetFiles(AppContext.BaseDirectory, "*.xml"))
    {
        c.IncludeXmlComments(xml, includeControllerXmlComments: true);
    }

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name        = "Authorization",
        Type        = SecuritySchemeType.Http,
        Scheme      = "bearer",
        BearerFormat = "JWT",
        In          = ParameterLocation.Header,
        Description = "Paste your JWT token here (without 'Bearer ' prefix)"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: ["db", "ready"]);

// ── Response Compression ──────────────────────────────────────────────────────
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// ── HTTP Logging ──────────────────────────────────────────────────────────────
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.RequestMethod
        | HttpLoggingFields.RequestPath
        | HttpLoggingFields.ResponseStatusCode
        | HttpLoggingFields.Duration;
});

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── OpenAPI Export ────────────────────────────────────────────────────────────
// Opt-in only: `dotnet run -- --dump-openapi[=path]` writes the spec to docs/api and exits
// without starting the server, so the committed document can be refreshed from the same
// pipeline that serves /swagger. Environment-agnostic: AddSwaggerGen is always registered,
// only the endpoint is Development-gated.
if (await OpenApiFileWriter.TryWriteAsync(app, args) is { } openApiExitCode)
    return openApiExitCode;

// ── Database Seeding ──────────────────────────────────────────────────────────
// Opt-in only: `dotnet run -- --seed` (or `--seed=catalog`) in Development. Never runs
// on a normal boot, and never outside Development, so production can't be seeded by
// accident. Goes through the same ISeedRunner as the /api/seed endpoints.
var seedArg = args.FirstOrDefault(a => a == "--seed" || a.StartsWith("--seed=", StringComparison.Ordinal));
if (seedArg is not null)
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("--seed is only permitted in the Development environment.");

    var setName = seedArg.Contains('=') ? seedArg.Split('=', 2)[1] : SeedSets.All;
    using var seedScope = app.Services.CreateScope();
    var report = await seedScope.ServiceProvider.GetRequiredService<ISeedRunner>().RunAsync(setName);

    Console.WriteLine(report.Success
        ? $"Seed '{report.Set}' completed: {string.Join(" -> ", report.StepsRun)}"
        : $"Seed '{report.Set}' FAILED after [{string.Join(" -> ", report.StepsRun)}]: {report.Error}");
    return report.Success ? 0 : 1;
}

// ── Middleware Pipeline ───────────────────────────────────────────────────────

// Global exception handler — must be first so it wraps everything
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpLogging();
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Black Ink Paper API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseHsts();
app.UseRouting();
app.UseRateLimiter();
app.UseCors("AllowAdminUI");
app.UseAuthentication();
app.UseMiddleware<TokenBlacklistMiddleware>();
app.UseAuthorization();
app.UseMiddleware<AdminAuditMiddleware>();

app.MapHealthChecks("/health");
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// Reached only on graceful shutdown; --seed returns earlier with its own exit code.
return 0;
