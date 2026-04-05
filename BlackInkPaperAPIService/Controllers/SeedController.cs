using Infrastructure.Persistence;
using Infrastructure.Persistence.Data_Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;

namespace BlackInkPaperAPIService.Controllers;

[ApiController]
[Route("api/seed")]
public class SeedController(
    IDapperContext dapperContext,
    IServiceProvider serviceProvider,
    IWebHostEnvironment env) : ControllerBase
{
    [HttpPost("all")]
    public async Task<IActionResult> SeedAll()
    {
        if (!env.IsDevelopment())
        {
            return Forbid("Seeding is only allowed in development.");
        }

        try
        {
            // 1. Clear Tables
            await ExecuteSqlScript("00_ClearTables.sql");

            // 2. Seed Identity (C# for Hashing)
            await SeedIdentity.SeedRolesAsync(serviceProvider);
            await SeedIdentity.SeedAdminUser(serviceProvider);
            
            // Seed a default artist user for testing catalog
            await SeedArtistUser("user-artist", "artist@artist.com", "Artist@123!");

            // 3. Seed Catalog & Commerce
            await ExecuteSqlScript("20_SeedCatalog.sql");
            await ExecuteSqlScript("30_SeedCommerce.sql");

            return Ok(new { Message = "Database cleared and seeded successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message, Details = ex.ToString() });
        }
    }

    
    [HttpPost("identity")]
    public async Task<IActionResult> SeedIdentities()
    {
        if (!env.IsDevelopment())
        {
            return Forbid("Seeding is only allowed in development.");
        }

        try
        {
            // 1. Clear Tables
            // await ExecuteSqlScript("00_ClearTables.sql");

            // 2. Seed Identity (C# for Hashing)
            await SeedIdentity.SeedRolesAsync(serviceProvider);
            await SeedIdentity.SeedAdminUser(serviceProvider);
            
            // Seed a default artist user for testing catalog
            await SeedArtistUser("user-artist", "artist@artist.com", "Artist@123!");

            // 3. Seed Catalog & Commerce
            // await ExecuteSqlScript("20_SeedCatalog.sql");
            // await ExecuteSqlScript("30_SeedCommerce.sql");

            return Ok(new { Message = "Database cleared and identity seeded successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message, Details = ex.ToString() });
        }
    }

    private async Task SeedArtistUser(string userId, string email, string password)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<AppIdentityUser>>();
        
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new AppIdentityUser
            {
                Id = userId, // Using specific ID to match SQL seeds if necessary
                UserName = email,
                Email = email,
                FullName = "Default Artist",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Artist");
            }
        }
    }

    private async Task ExecuteSqlScript(string scriptName)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "..", "Infrastructure", "Persistence", "Seeds", scriptName);
        if (!System.IO.File.Exists(path))
        {
            // Try alternative path if needed (depending on where Directory.GetCurrentDirectory() points)
             path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Persistence", "Seeds", scriptName);
        }
        
        // Fallback for different build environments
        if (!System.IO.File.Exists(path))
        {
             var root = FindProjectRoot(Directory.GetCurrentDirectory());
             path = Path.Combine(root, "Infrastructure", "Persistence", "Seeds", scriptName);
        }

        var sql = await System.IO.File.ReadAllTextAsync(path);
        
        using var connection = dapperContext.CreateConnection();
        connection.Open();
        
        // Split by GO if necessary, but Dapper can handle multiple statements usually.
        // However, some scripts might have GO which SqlClient doesn't like.
        var batches = sql.Split(new[] { "GO", "go", "Go", "gO" }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch)) continue;
            await connection.ExecuteAsync(batch);
        }
    }

    private static string FindProjectRoot(string currentDir)
    {
        var dir = new DirectoryInfo(currentDir);
        while (dir != null && !dir.GetFiles("*.sln").Any())
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? currentDir;
    }
}
