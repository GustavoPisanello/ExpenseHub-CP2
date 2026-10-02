using System;
using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Endpoints;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
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
        builder.Services.AddOpenApi(options => options.AddDocumentTransformer(OpenApiSecurity.AddBearerAsync));
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<AppExceptionHandler>();
        builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

        string connectionString = builder.Configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("A connection string 'Default' não foi configurada.");
        builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        builder.Services.AddIdentityCore<IdentityUser>(options => options.User.RequireUniqueEmail = true)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager();

        builder.Services.AddAuthentication(IdentityConstants.BearerScheme)
            .AddBearerToken(IdentityConstants.BearerScheme);
        builder.Services.AddAuthorization();

        builder.Services.AddScoped<UserAdminService>();

        WebApplication app = builder.Build();

        await DatabaseInitializer.InitializeAsync(app.Services);

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "ExpenseHub v1"));
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithName("GetHealth");
        app.MapAuthEndpoints();
        app.MapAdminUserEndpoints();

        await app.RunAsync();
    }
}
