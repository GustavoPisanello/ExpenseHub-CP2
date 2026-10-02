using System;
using System.Linq;
using ExpenseHub.Api.Models;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

[TestClass]
internal sealed class ExpenseSubmitTests
{
    private const string OwnerId = "owner-id";
    private const string OtherUserId = "other-id";

    private static readonly DateTime _createdAt = new(2026, 10, 1, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _submittedAt = _createdAt.AddHours(2);

    [TestMethod]
    public void Submit_ByOwnerInDraft_MovesToSubmitted()
    {
        Expense expense = CreateDraft();
        Guid originalVersion = expense.Version;

        expense.Submit(OwnerId, _submittedAt);

        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
        Assert.AreEqual(_submittedAt, expense.UpdatedAtUtc);
        Assert.AreNotEqual(originalVersion, expense.Version);
    }

    [TestMethod]
    public void Submit_ByOwnerInDraft_RecordsSubmittedHistory()
    {
        Expense expense = CreateDraft();

        expense.Submit(OwnerId, _submittedAt);

        ExpenseHistory history = expense.History.Last();
        Assert.AreEqual(ExpenseAction.Submitted, history.Action);
        Assert.AreEqual(OwnerId, history.ActorId);
        Assert.AreEqual(_submittedAt, history.OccurredAtUtc);
        Assert.AreEqual(ExpenseStatus.Draft, history.FromStatus);
        Assert.AreEqual(ExpenseStatus.Submitted, history.ToStatus);
    }

    [TestMethod]
    public void Submit_Twice_ThrowsConflictWithoutDuplicatingHistory()
    {
        Expense expense = CreateDraft();
        expense.Submit(OwnerId, _submittedAt);

        AppException exception = Assert.ThrowsExactly<AppException>(() => expense.Submit(OwnerId, _submittedAt.AddMinutes(1)));

        Assert.AreEqual(StatusCodes.Status409Conflict, exception.StatusCode);
        Assert.HasCount(2, expense.History);
        Assert.AreEqual(_submittedAt, expense.UpdatedAtUtc);
    }

    [TestMethod]
    public void Submit_ByAnotherUser_ThrowsNotFoundAndKeepsDraft()
    {
        Expense expense = CreateDraft();

        AppException exception = Assert.ThrowsExactly<AppException>(() => expense.Submit(OtherUserId, _submittedAt));

        Assert.AreEqual(StatusCodes.Status404NotFound, exception.StatusCode);
        Assert.AreEqual(ExpenseStatus.Draft, expense.Status);
        Assert.HasCount(1, expense.History);
    }

    [TestMethod]
    [DataRow(ExpenseStatus.Approved)]
    [DataRow(ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Paid)]
    public void Submit_WhenNotDraft_ThrowsConflictAndKeepsStatus(ExpenseStatus status)
    {
        Expense expense = CreateDraft();
        expense.Status = status;

        AppException exception = Assert.ThrowsExactly<AppException>(() => expense.Submit(OwnerId, _submittedAt));

        Assert.AreEqual(StatusCodes.Status409Conflict, exception.StatusCode);
        Assert.AreEqual(status, expense.Status);
        Assert.HasCount(1, expense.History);
    }

    private static Expense CreateDraft() =>
        Expense.CreateDraft(OwnerId, "Almoço com cliente", 120.50m, new DateOnly(2026, 9, 30), 1, _createdAt);
}
