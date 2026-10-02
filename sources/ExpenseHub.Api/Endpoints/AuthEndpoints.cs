using System.Linq;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts;
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
        app.MapPost("/register", RegisterAsync)
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
            .AllowAnonymous();

        app.MapPost("/login", LoginAsync)
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .AllowAnonymous();
    }

    private static async Task<IResult> RegisterAsync(RegisterRequest request, UserManager<IdentityUser> userManager)
    {
        IdentityUser user = new()
        {
            UserName = request.Email,
            Email = request.Email,
        };

        IdentityResult result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return Results.ValidationProblem(result.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray()));
        }

        return Results.Created((string?)null, new UserResponse(user.Id, request.Email, []));
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
