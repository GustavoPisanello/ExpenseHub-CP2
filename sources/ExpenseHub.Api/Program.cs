using System;
using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ExpenseHub.Api;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.Services.AddOpenApi();

        string connectionString = builder.Configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("A connection string 'Default' não foi configurada.");
        builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        WebApplication app = builder.Build();

        await DatabaseInitializer.InitializeAsync(app.Services);

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithName("GetHealth");

        await app.RunAsync();
    }
}
