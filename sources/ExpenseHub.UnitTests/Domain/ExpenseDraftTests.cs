using System;
using System.Linq;
using ExpenseHub.Api.Models;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

[TestClass]
internal sealed class ExpenseDraftTests
{
    private const string OwnerId = "owner-id";
    private const string OtherUserId = "other-id";

    private static readonly DateTime _now = new(2026, 10, 1, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly _today = new(2026, 10, 1);

    [TestMethod]
    public void CreateDraft_WithValidData_CreatesDraftOwnedByCreator()
    {
        Expense expense = CreateDraft();

        Assert.AreEqual(ExpenseStatus.Draft, expense.Status);
        Assert.AreEqual(OwnerId, expense.OwnerId);
        Assert.AreNotEqual(Guid.Empty, expense.Id);
        Assert.AreEqual(_now, expense.CreatedAtUtc);
        Assert.AreEqual(_now, expense.UpdatedAtUtc);
    }

    [TestMethod]
    public void CreateDraft_RecordsCreatedHistoryWithServerActorAndTime()
    {
        Expense expense = CreateDraft();

        ExpenseHistory history = expense.History.Single();
        Assert.AreEqual(ExpenseAction.Created, history.Action);
        Assert.AreEqual(OwnerId, history.ActorId);
        Assert.AreEqual(_now, history.OccurredAtUtc);
        Assert.IsNull(history.FromStatus);
        Assert.AreEqual(ExpenseStatus.Draft, history.ToStatus);
    }

    [TestMethod]
    public void CreateDraft_WithTodayAsExpenseDate_IsAccepted()
    {
        Expense expense = Expense.CreateDraft(OwnerId, "Táxi para o aeroporto", 50m, _today, 2, _now);

        Assert.AreEqual(_today, expense.ExpenseDate);
    }

    [TestMethod]
    public void CreateDraft_WithFutureExpenseDate_ThrowsBadRequest()
    {
        AppException exception = Assert.ThrowsExactly<AppException>(
            () => Expense.CreateDraft(OwnerId, "Táxi para o aeroporto", 50m, _today.AddDays(1), 2, _now));

        Assert.AreEqual(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [TestMethod]
    public void UpdateDraft_ByOwner_UpdatesFieldsAndKeepsDraft()
    {
        Expense expense = CreateDraft();
        Guid originalVersion = expense.Version;
        DateTime later = _now.AddHours(1);

        expense.UpdateDraft(OwnerId, "Jantar com fornecedor", 80m, _today.AddDays(-2), 3, later);

        Assert.AreEqual("Jantar com fornecedor", expense.Description);
        Assert.AreEqual(80m, expense.Amount);
        Assert.AreEqual(_today.AddDays(-2), expense.ExpenseDate);
        Assert.AreEqual(3, expense.CategoryId);
        Assert.AreEqual(ExpenseStatus.Draft, expense.Status);
        Assert.AreEqual(later, expense.UpdatedAtUtc);
        Assert.AreNotEqual(originalVersion, expense.Version);
    }

    [TestMethod]
    public void UpdateDraft_ByOwner_RecordsUpdatedHistoryWithChanges()
    {
        Expense expense = CreateDraft();

        expense.UpdateDraft(OwnerId, "Almoço com cliente", 99.90m, _today.AddDays(-1), 1, _now);

        ExpenseHistory history = expense.History.Last();
        Assert.AreEqual(ExpenseAction.Updated, history.Action);
        Assert.AreEqual(OwnerId, history.ActorId);
        Assert.AreEqual(ExpenseStatus.Draft, history.FromStatus);
        Assert.AreEqual(ExpenseStatus.Draft, history.ToStatus);
        Assert.AreEqual("Amount: 120.50 -> 99.90", history.Changes);
    }

    [TestMethod]
    public void UpdateDraft_WithoutChanges_DoesNotRecordHistory()
    {
        Expense expense = CreateDraft();

        expense.UpdateDraft(OwnerId, "Almoço com cliente", 120.50m, _today.AddDays(-1), 1, _now.AddHours(1));

        Assert.HasCount(1, expense.History);
        Assert.AreEqual(_now, expense.UpdatedAtUtc);
    }

    [TestMethod]
    public void UpdateDraft_ByAnotherUser_ThrowsNotFoundAndKeepsExpenseUnchanged()
    {
        Expense expense = CreateDraft();

        AppException exception = Assert.ThrowsExactly<AppException>(
            () => expense.UpdateDraft(OtherUserId, "Alteração indevida", 1m, _today, 2, _now));

        Assert.AreEqual(StatusCodes.Status404NotFound, exception.StatusCode);
        Assert.AreEqual("Almoço com cliente", expense.Description);
        Assert.HasCount(1, expense.History);
    }

    [TestMethod]
    [DataRow(ExpenseStatus.Submitted)]
    [DataRow(ExpenseStatus.Approved)]
    [DataRow(ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Paid)]
    public void UpdateDraft_WhenNotDraft_ThrowsConflict(ExpenseStatus status)
    {
        Expense expense = CreateDraft();
        expense.Status = status;

        AppException exception = Assert.ThrowsExactly<AppException>(
            () => expense.UpdateDraft(OwnerId, "Jantar com fornecedor", 80m, _today, 3, _now));

        Assert.AreEqual(StatusCodes.Status409Conflict, exception.StatusCode);
        Assert.AreEqual("Almoço com cliente", expense.Description);
        Assert.HasCount(1, expense.History);
    }

    [TestMethod]
    public void UpdateDraft_WithFutureExpenseDate_ThrowsBadRequest()
    {
        Expense expense = CreateDraft();

        AppException exception = Assert.ThrowsExactly<AppException>(
            () => expense.UpdateDraft(OwnerId, "Almoço com cliente", 120.50m, _today.AddDays(1), 1, _now));

        Assert.AreEqual(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    private static Expense CreateDraft() =>
        Expense.CreateDraft(OwnerId, "Almoço com cliente", 120.50m, _today.AddDays(-1), 1, _now);
}
