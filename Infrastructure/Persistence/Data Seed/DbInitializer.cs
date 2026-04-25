using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
// using Microsoft.AspNetCore.Hosting;
// using Microsoft.Extensions.Hosting;
using Dapper;
using System.Data;
using Infrastructure.Persistence;

namespace Infrastructure.Persistence.Data_Seed
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            // var logger = serviceProvider.GetRequiredService<ILogger<typeof(SeedIdentity)>>();
            // var env = serviceProvider.GetRequiredService<IWebHostEnvironment>();
            //
            // if (!env.IsDevelopment())
            // {
            //     logger.LogInformation("Seeding skipped. Not in Development environment.");
            //     return;
            // }

            try
            {
                // logger.LogInformation("Starting database seeding...");

                // 1. Seed Roles and Users (C#)
                Console.WriteLine("Starting database seeding...");
                
                await SeedIdentity.SeedRolesAsync(serviceProvider);
                await SeedIdentity.SeedAdminUserAsync(serviceProvider);
                await SeedIdentity.SeedArtistUserAsync(serviceProvider);
                
                // logger.LogInformation("Identity seeding completed.");

                // 2. Run SQL Seeds if needed
                // Note: In a real app, you might want to check if data already exists
                // or use a flag to decide whether to run SQL seeds.
                // For now, let's provide the capability.
                await ExecuteSqlSeedAsync(serviceProvider, "SeedAll.Postgres.sql");
                Console.WriteLine("Database seeding completed successfully.");
                // logger.LogInformation("Database seeding completed successfully.");
            }
            catch (Exception ex)
            {
                // logger.LogError(ex, "An error occurred during database seeding.");
                throw;
            }
        }

        private static async Task ExecuteSqlSeedAsync(IServiceProvider serviceProvider, string scriptName)
        {
            var dapperContext = serviceProvider.GetRequiredService<IDapperContext>();
            // var logger = serviceProvider.GetRequiredService<ILogger<typeof(SeedIdentity)>>();
            
            // Resolve path to the script
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Persistence", "Seeds", scriptName);
            
            // Fallback for development (checking relative to project root)
            if (!File.Exists(path))
            {
                 var currentDir = Directory.GetCurrentDirectory();
                 // Try to find the Infrastructure project directory
                 var infrastructurePath = Path.Combine(currentDir, "..", "Infrastructure", "Persistence", "Seeds", scriptName);
                 if (File.Exists(infrastructurePath))
                 {
                     path = infrastructurePath;
                 }
            }

            if (!File.Exists(path))
            {
                // logger.LogWarning("SQL seed script not found at {Path}. Skipping SQL seeding.", path);
                return;
            }

            var sql = await File.ReadAllTextAsync(path);
            
            using var connection = dapperContext.CreateConnection();
            if (connection.State != ConnectionState.Open) connection.Open();
            
            using var transaction = connection.BeginTransaction();
            try
            {
                // Dapper can handle multiple statements in one call, but splitting by transaction blocks in script is better.
                // Our Postgres script has BEGIN; and COMMIT;
                await connection.ExecuteAsync(sql, transaction: transaction);
                transaction.Commit();
                // logger.LogInformation("Successfully executed SQL script: {ScriptName}", scriptName);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                // logger.LogError(ex, "Failed to execute SQL script: {ScriptName}", scriptName);
                // We might not want to throw here if we want to allow the app to start even if SQL seeding fails
            }
        }
    }
}
