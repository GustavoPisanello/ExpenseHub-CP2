using System.Threading.Tasks;
using ExpenseHub.Api.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace ExpenseHub.Api.Endpoints;

internal static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/login", LoginAsync)
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .AllowAnonymous();
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, SignInManager<IdentityUser> signInManager)
    {
        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

        SignInResult result = await signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Results.Problem(
                title: "Credenciais inválidas.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return Results.Empty;
    }
}
