using System;
using Microsoft.AspNetCore.Http;

namespace ExpenseHub.Api.Services;

internal sealed class AppException : Exception
{
    public AppException()
    {
    }

    public AppException(string message)
        : base(message)
    {
    }

    public AppException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    private AppException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; } = StatusCodes.Status500InternalServerError;

    public static AppException BadRequest(string message) => new(StatusCodes.Status400BadRequest, message);

    public static AppException Forbidden(string message) => new(StatusCodes.Status403Forbidden, message);

    public static AppException NotFound(string message) => new(StatusCodes.Status404NotFound, message);

    public static AppException Conflict(string message) => new(StatusCodes.Status409Conflict, message);
}
