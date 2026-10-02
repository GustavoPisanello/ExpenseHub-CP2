using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace ExpenseHub.Api.Endpoints;

internal static class ExpenseEndpoints
{
    public static void MapExpenseEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/expenses")
            .RequireAuthorization();

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Employee))
            .AddEndpointFilter<ValidationFilter<ExpenseRequest>>();

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Employee))
            .AddEndpointFilter<ValidationFilter<ExpenseRequest>>();
    }

    private static async Task<IResult> CreateAsync(
        ExpenseRequest request,
        ClaimsPrincipal principal,
        UserManager<IdentityUser> userManager,
        ExpenseService service,
        CancellationToken cancellationToken)
    {
        ExpenseResponse expense = await service.CreateAsync(GetUserId(principal, userManager), request, cancellationToken);
        return Results.Created($"/api/expenses/{expense.Id}", expense);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        ExpenseRequest request,
        ClaimsPrincipal principal,
        UserManager<IdentityUser> userManager,
        ExpenseService service,
        CancellationToken cancellationToken)
    {
        ExpenseResponse expense = await service.UpdateAsync(GetUserId(principal, userManager), id, request, cancellationToken);
        return Results.Ok(expense);
    }

    private static string GetUserId(ClaimsPrincipal principal, UserManager<IdentityUser> userManager)
        => userManager.GetUserId(principal) ?? throw new InvalidOperationException("Usuário autenticado sem identificador.");
}
