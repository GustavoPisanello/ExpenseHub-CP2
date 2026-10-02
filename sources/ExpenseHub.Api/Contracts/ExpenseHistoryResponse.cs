using System;
using ExpenseHub.Api.Models;

namespace ExpenseHub.Api.Contracts;

internal sealed record ExpenseHistoryResponse(
    Guid Id,
    Guid ExpenseId,
    string Action,
    string ActorId,
    DateTime OccurredAtUtc,
    string? FromStatus,
    string ToStatus,
    string? Justification,
    string? Changes)
{
    public static ExpenseHistoryResponse From(ExpenseHistory history) => new(
        history.Id,
        history.ExpenseId,
        history.Action.ToString(),
        history.ActorId,
        history.OccurredAtUtc,
        history.FromStatus?.ToString(),
        history.ToStatus.ToString(),
        history.Justification,
        history.Changes);
}
