using System;
using System.Collections.Generic;
using System.Globalization;
using ExpenseHub.Api.Services;

namespace ExpenseHub.Api.Models;

internal sealed class Expense
{
    public Guid Id { get; set; }

    public string OwnerId { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public ExpenseCategory? Category { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateOnly ExpenseDate { get; set; }

    public ExpenseStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Guid Version { get; set; }

    public List<ExpenseHistory> History { get; set; } = [];

    public PaymentRecord? Payment { get; set; }

    public static Expense CreateDraft(string ownerId, string description, decimal amount, DateOnly expenseDate, int categoryId, DateTime nowUtc)
    {
        EnsureNotFuture(expenseDate, nowUtc);

        Expense expense = new()
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Description = description.Trim(),
            Amount = amount,
            ExpenseDate = expenseDate,
            CategoryId = categoryId,
            Status = ExpenseStatus.Draft,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            Version = Guid.NewGuid(),
        };

        expense.AddHistory(ExpenseAction.Created, ownerId, nowUtc, fromStatus: null);
        return expense;
    }

    public void UpdateDraft(string actorId, string description, decimal amount, DateOnly expenseDate, int categoryId, DateTime nowUtc)
    {
        if (actorId != OwnerId)
        {
            throw AppException.NotFound("Reembolso não encontrado.");
        }

        if (Status != ExpenseStatus.Draft)
        {
            throw AppException.Conflict("Somente reembolsos em Draft podem ser editados.");
        }

        EnsureNotFuture(expenseDate, nowUtc);

        description = description.Trim();
        List<string> changes = [];
        if (Description != description)
        {
            changes.Add($"Description: '{Description}' -> '{description}'");
        }

        if (Amount != amount)
        {
            changes.Add(string.Create(CultureInfo.InvariantCulture, $"Amount: {Amount} -> {amount}"));
        }

        if (ExpenseDate != expenseDate)
        {
            changes.Add(string.Create(CultureInfo.InvariantCulture, $"ExpenseDate: {ExpenseDate:yyyy-MM-dd} -> {expenseDate:yyyy-MM-dd}"));
        }

        if (CategoryId != categoryId)
        {
            changes.Add(string.Create(CultureInfo.InvariantCulture, $"CategoryId: {CategoryId} -> {categoryId}"));
        }

        if (changes.Count == 0)
        {
            return;
        }

        Description = description;
        Amount = amount;
        ExpenseDate = expenseDate;
        CategoryId = categoryId;
        UpdatedAtUtc = nowUtc;
        Version = Guid.NewGuid();

        AddHistory(ExpenseAction.Updated, actorId, nowUtc, ExpenseStatus.Draft, string.Join("; ", changes));
    }

    private static void EnsureNotFuture(DateOnly expenseDate, DateTime nowUtc)
    {
        if (expenseDate > DateOnly.FromDateTime(nowUtc))
        {
            throw AppException.BadRequest("A data da despesa não pode ser futura.");
        }
    }

    private void AddHistory(ExpenseAction action, string actorId, DateTime nowUtc, ExpenseStatus? fromStatus, string? changes = null)
    {
        History.Add(new ExpenseHistory
        {
            ExpenseId = Id,
            Action = action,
            ActorId = actorId,
            OccurredAtUtc = nowUtc,
            FromStatus = fromStatus,
            ToStatus = Status,
            Changes = changes,
        });
    }
}
