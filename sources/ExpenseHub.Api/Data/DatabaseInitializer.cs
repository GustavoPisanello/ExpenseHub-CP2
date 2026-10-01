using System;
using System.Linq;
using System.Threading.Tasks;
using ExpenseHub.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseHub.Api.Data;

internal static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        IServiceProvider provider = scope.ServiceProvider;

        AppDbContext context = provider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();

        await SeedRolesAsync(provider.GetRequiredService<RoleManager<IdentityRole>>());
        await SeedAdminAsync(
            provider.GetRequiredService<UserManager<IdentityUser>>(),
            provider.GetRequiredService<IConfiguration>());
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (string role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole(role)));
            }
        }
    }

    private static async Task SeedAdminAsync(UserManager<IdentityUser> userManager, IConfiguration configuration)
    {
        string adminEmail = configuration["Seed:AdminEmail"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            throw new InvalidOperationException("Configure 'Seed:AdminEmail' para criar a conta Admin inicial.");
        }

        IdentityUser? admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            string adminPassword = configuration["Seed:AdminPassword"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new InvalidOperationException(
                    "Configure 'Seed:AdminPassword' com dotnet user-secrets ou com a variável de ambiente Seed__AdminPassword.");
            }

            admin = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
            };
            EnsureSucceeded(await userManager.CreateAsync(admin, adminPassword));
        }

        if (!await userManager.IsInRoleAsync(admin, Roles.Admin))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(admin, Roles.Admin));
        }
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            string errors = string.Join(" ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Falha ao executar o seed: {errors}");
        }
    }
}
