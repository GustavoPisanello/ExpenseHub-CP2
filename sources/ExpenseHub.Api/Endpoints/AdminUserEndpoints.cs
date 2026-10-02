using System.Collections.Generic;
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

internal static class AdminUserEndpoints
{
    public static void MapAdminUserEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/admin/users")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

        group.MapGet("/", ListUsersAsync);

        group.MapPut("/{id}/roles", UpdateRolesAsync)
            .AddEndpointFilter<ValidationFilter<UpdateUserRolesRequest>>();
    }

    private static async Task<IResult> ListUsersAsync(UserAdminService service, CancellationToken cancellationToken)
    {
        IReadOnlyList<UserResponse> users = await service.ListUsersAsync(cancellationToken);
        return Results.Ok(users);
    }

    private static async Task<IResult> UpdateRolesAsync(
        string id,
        UpdateUserRolesRequest request,
        ClaimsPrincipal principal,
        UserManager<IdentityUser> userManager,
        UserAdminService service,
        CancellationToken cancellationToken)
    {
        string currentUserId = userManager.GetUserId(principal) ?? string.Empty;
        UserResponse user = await service.UpdateRolesAsync(currentUserId, id, request.Roles ?? [], cancellationToken);
        return Results.Ok(user);
    }
}
