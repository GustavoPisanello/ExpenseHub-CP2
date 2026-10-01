using System;

namespace ExpenseHub.Api.Models;

internal sealed class ExpenseHistory
{
    public Guid Id { get; set; }

    public Guid ExpenseId { get; set; }

    public ExpenseAction Action { get; set; }

    public string ActorId { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }

    public ExpenseStatus? FromStatus { get; set; }

    public ExpenseStatus ToStatus { get; set; }

    public string? Justification { get; set; }

    public string? Changes { get; set; }
}
