using BlackInkPaperAPIService.Middleware;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Contracts.Services;
using Infrastructure.Configuration;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Data_Seed;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// CORS for Admin UI
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAdminUI", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

//DB Setup [Dapper]
builder.Services.AddSingleton<IDapperContext, DapperContext>();
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;


// User Identity [EF Core Setup - Recommended for Identity tables]
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found.");
builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddIdentity<AppIdentityUser, IdentityRole>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppIdentityDbContext>()
.AddDefaultTokenProviders();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddProblemDetails();
builder.Services.Configure<CheckoutPricingOptions>(builder.Configuration.GetSection("CheckoutPricing"));
builder.Services.Configure<RazorpayOptions>(builder.Configuration.GetSection("Razorpay"));
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
builder.Services.AddHttpClient<IRazorpayGateway, RazorpayGateway>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RazorpayOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

var app = builder.Build();

// Database Seeding
// using (var scope = app.Services.CreateScope())
// {
//     var services = scope.ServiceProvider;
//     await DbInitializer.InitializeAsync(services);
// }


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAdminUI");
app.UseAuthentication();
app.UseMiddleware<TokenBlacklistMiddleware>();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
