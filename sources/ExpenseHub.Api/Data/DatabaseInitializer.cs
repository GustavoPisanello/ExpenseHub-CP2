using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseHub.Api.Data;

internal static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }
}
