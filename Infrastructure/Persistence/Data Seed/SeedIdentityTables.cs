using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Persistence;

namespace Infrastructure.Persistence.Data_Seed
{
    public static class SeedIdentity {
        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            string[] roleNames = { "Admin", "Artist", "User" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }
        public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppIdentityUser>>();

            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            // 2. Create the Admin User
            var adminEmail = "admin@admin.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var user = new AppIdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, "Admin@123!"); // Use a strong password!

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }
        }
        public static async Task SeedArtistUserAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<AppIdentityUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            if (!await roleManager.RoleExistsAsync("Artist"))
                await roleManager.CreateAsync(new IdentityRole("Artist"));

            var artistEmail = "artist@artist.com";
            var artistUser = await userManager.FindByEmailAsync(artistEmail);

            if (artistUser == null)
            {
                var user = new AppIdentityUser
                {
                    Id = "user-artist", // Fixed ID to match SQL seeds
                    UserName = artistEmail,
                    Email = artistEmail,
                    FullName = "Aarav Kapoor",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, "Artist@123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Artist");
                }
            }
        }
    }
}


