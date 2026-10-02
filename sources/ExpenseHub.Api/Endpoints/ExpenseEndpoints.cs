using System;
using System.Collections.Generic;
using System.Linq;
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

        group.MapGet("/", ListAsync)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Employee, Roles.Approver, Roles.Finance, Roles.Auditor));

        group.MapGet("/{id:guid}", GetAsync)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Employee, Roles.Approver, Roles.Finance, Roles.Auditor));

        group.MapPost("/{id:guid}/submit", SubmitAsync)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Employee));

        group.MapPost("/{id:guid}/approve", ApproveAsync)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Approver));

        group.MapPost("/{id:guid}/reject", RejectAsync)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Approver))
            .AddEndpointFilter<ValidationFilter<RejectExpenseRequest>>();

        group.MapPost("/{id:guid}/pay", PayAsync)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Finance));

        group.MapGet("/{id:guid}/history", GetHistoryAsync)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Employee, Roles.Approver, Roles.Finance, Roles.Auditor));
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

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        UserManager<IdentityUser> userManager,
        ExpenseService service,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ExpenseResponse> expenses = await service.ListAsync(GetUserId(principal, userManager), GetRoles(principal), cancellationToken);
        return Results.Ok(expenses);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ClaimsPrincipal principal,
        UserManager<IdentityUser> userManager,
        ExpenseService service,
        CancellationToken cancellationToken)
    {
        ExpenseResponse expense = await service.GetAsync(GetUserId(principal, userManager), GetRoles(principal), id, cancellationToken);
        return Results.Ok(expense);
    }

    private static async Task<IResult> SubmitAsync(
        Guid id,
        ClaimsPrincipal principal,
        UserManager<IdentityUser> userManager,
        ExpenseService service,
        CancellationToken cancellationToken)
    {
        ExpenseResponse expense = await service.SubmitAsync(GetUserId(principal, userManager), id, cancellationToken);
        return Results.Ok(expense);
    }

    private static async Task<IResult> ApproveAsync(
        Guid id,
        ClaimsPrincipal principal,
        UserManager<IdentityUser> userManager,
        ExpenseService service,
        CancellationToken cancellationToken)
    {
        ExpenseResponse expense = await service.ApproveAsync(GetUserId(principal, userManager), id, cancellationToken);
        return Results.Ok(expense);
    }

    private static async Task<IResult> RejectAsync(
        Guid id,
        RejectExpenseRequest request,
        ClaimsPrincipal principal,
        UserManager<IdentityUser> userManager,
        ExpenseService service,
        CancellationToken cancellationToken)
    {
        ExpenseResponse expense = await service.RejectAsync(GetUserId(principal, userManager), id, request, cancellationToken);
        return Results.Ok(expense);
    }

    private static async Task<IResult> PayAsync(
        Guid id,
        ClaimsPrincipal principal,
        UserManager<IdentityUser> userManager,
        ExpenseService service,
        CancellationToken cancellationToken)
    {
        ExpenseResponse expense = await service.PayAsync(GetUserId(principal, userManager), id, cancellationToken);
        return Results.Ok(expense);
    }

    private static async Task<IResult> GetHistoryAsync(
        Guid id,
        ClaimsPrincipal principal,
        UserManager<IdentityUser> userManager,
        ExpenseService service,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ExpenseHistoryResponse> history = await service.GetHistoryAsync(GetUserId(principal, userManager), GetRoles(principal), id, cancellationToken);
        return Results.Ok(history);
    }

    private static List<string> GetRoles(ClaimsPrincipal principal)
        => Roles.All.Where(principal.IsInRole).ToList();

    private static string GetUserId(ClaimsPrincipal principal, UserManager<IdentityUser> userManager)
        => userManager.GetUserId(principal) ?? throw new InvalidOperationException("Usuário autenticado sem identificador.");
}
