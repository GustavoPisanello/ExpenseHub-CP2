using System;
using ExpenseHub.Api.Models;

namespace ExpenseHub.Api.Contracts;

internal sealed record ExpenseResponse(
    Guid Id,
    string OwnerId,
    int CategoryId,
    string Description,
    decimal Amount,
    DateOnly ExpenseDate,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public static ExpenseResponse From(Expense expense) => new(
        expense.Id,
        expense.OwnerId,
        expense.CategoryId,
        expense.Description,
        expense.Amount,
        expense.ExpenseDate,
        expense.Status.ToString(),
        expense.CreatedAtUtc,
        expense.UpdatedAtUtc);
}
