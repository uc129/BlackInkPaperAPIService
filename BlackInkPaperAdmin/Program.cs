using BlackInkPaperAdmin.Components;
using BlackInkPaperAdmin.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<AdminApiOptions>(builder.Configuration.GetSection("AdminApi"));
builder.Services.AddScoped<AdminSession>();
builder.Services.AddScoped<AuthApiClient>();
builder.Services.AddScoped<ProductAdminApiClient>();
builder.Services.AddScoped(_ => new HttpClient());


// In Program.cs
builder.Services.AddLocalStorageServices();
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options =>
    {
        options.DetailedErrors = true;
    });

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = true;
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}



app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
