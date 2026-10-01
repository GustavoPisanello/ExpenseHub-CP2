using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ExpenseHub.Api.Endpoints;

internal sealed class ValidationFilter<T> : IEndpointFilter
    where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        T? request = context.Arguments.OfType<T>().FirstOrDefault();
        if (request is null)
        {
            return Results.Problem(
                title: "O corpo da requisição é obrigatório.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        List<ValidationResult> results = [];
        if (!Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true))
        {
            Dictionary<string, string[]> errors = results
                .SelectMany(result => result.MemberNames.Select(member => (member, message: result.ErrorMessage ?? string.Empty)))
                .GroupBy(error => error.member)
                .ToDictionary(group => group.Key, group => group.Select(error => error.message).ToArray());

            return Results.ValidationProblem(errors);
        }

        return await next(context);
    }
}
