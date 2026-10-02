using System;
using System.Linq;
using ExpenseHub.Api.Models;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

[TestClass]
internal sealed class ExpenseDecisionTests
{
    private const string OwnerId = "owner-id";
    private const string ApproverId = "approver-id";
    private const string Justification = "Comprovante ilegível e sem CNPJ.";

    private static readonly DateTime _createdAt = new(2026, 10, 1, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _decidedAt = _createdAt.AddHours(5);

    [TestMethod]
    public void Approve_SubmittedByAnotherUser_MovesToApproved()
    {
        Expense expense = CreateSubmitted();
        Guid originalVersion = expense.Version;

        expense.Approve(ApproverId, _decidedAt);

        Assert.AreEqual(ExpenseStatus.Approved, expense.Status);
        Assert.AreEqual(_decidedAt, expense.UpdatedAtUtc);
        Assert.AreNotEqual(originalVersion, expense.Version);
    }

    [TestMethod]
    public void Approve_RecordsApprovedHistoryWithActorAndTime()
    {
        Expense expense = CreateSubmitted();

        expense.Approve(ApproverId, _decidedAt);

        ExpenseHistory history = expense.History.Last();
        Assert.AreEqual(ExpenseAction.Approved, history.Action);
        Assert.AreEqual(ApproverId, history.ActorId);
        Assert.AreEqual(_decidedAt, history.OccurredAtUtc);
        Assert.AreEqual(ExpenseStatus.Submitted, history.FromStatus);
        Assert.AreEqual(ExpenseStatus.Approved, history.ToStatus);
        Assert.IsNull(history.Justification);
    }

    [TestMethod]
    public void Approve_WhenActorIsOwner_ThrowsForbiddenAndKeepsSubmitted()
    {
        Expense expense = CreateSubmitted();

        AppException exception = Assert.ThrowsExactly<AppException>(() => expense.Approve(OwnerId, _decidedAt));

        Assert.AreEqual(StatusCodes.Status403Forbidden, exception.StatusCode);
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
        Assert.HasCount(2, expense.History);
    }

    [TestMethod]
    public void Approve_WhenDraft_ThrowsNotFoundEvenForOwner()
    {
        Expense expense = CreateDraft();

        AppException byOther = Assert.ThrowsExactly<AppException>(() => expense.Approve(ApproverId, _decidedAt));
        AppException byOwner = Assert.ThrowsExactly<AppException>(() => expense.Approve(OwnerId, _decidedAt));

        Assert.AreEqual(StatusCodes.Status404NotFound, byOther.StatusCode);
        Assert.AreEqual(StatusCodes.Status404NotFound, byOwner.StatusCode);
        Assert.AreEqual(ExpenseStatus.Draft, expense.Status);
    }

    [TestMethod]
    [DataRow(ExpenseStatus.Approved)]
    [DataRow(ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Paid)]
    public void Approve_WhenAlreadyDecided_ThrowsConflictWithoutHistory(ExpenseStatus status)
    {
        Expense expense = CreateSubmitted();
        expense.Status = status;

        AppException exception = Assert.ThrowsExactly<AppException>(() => expense.Approve(ApproverId, _decidedAt));

        Assert.AreEqual(StatusCodes.Status409Conflict, exception.StatusCode);
        Assert.AreEqual(status, expense.Status);
        Assert.HasCount(2, expense.History);
    }

    [TestMethod]
    public void Approve_Twice_ThrowsConflictWithoutDuplicatingHistory()
    {
        Expense expense = CreateSubmitted();
        expense.Approve(ApproverId, _decidedAt);

        AppException exception = Assert.ThrowsExactly<AppException>(() => expense.Approve(ApproverId, _decidedAt.AddMinutes(1)));

        Assert.AreEqual(StatusCodes.Status409Conflict, exception.StatusCode);
        Assert.HasCount(3, expense.History);
        Assert.AreEqual(_decidedAt, expense.UpdatedAtUtc);
    }

    [TestMethod]
    public void Reject_SubmittedByAnotherUser_MovesToRejectedAndStoresTrimmedJustification()
    {
        Expense expense = CreateSubmitted();

        expense.Reject(ApproverId, $"  {Justification}  ", _decidedAt);

        ExpenseHistory history = expense.History.Last();
        Assert.AreEqual(ExpenseStatus.Rejected, expense.Status);
        Assert.AreEqual(ExpenseAction.Rejected, history.Action);
        Assert.AreEqual(ApproverId, history.ActorId);
        Assert.AreEqual(_decidedAt, history.OccurredAtUtc);
        Assert.AreEqual(ExpenseStatus.Submitted, history.FromStatus);
        Assert.AreEqual(ExpenseStatus.Rejected, history.ToStatus);
        Assert.AreEqual(Justification, history.Justification);
    }

    [TestMethod]
    public void Reject_WhenActorIsOwner_ThrowsForbiddenAndKeepsSubmitted()
    {
        Expense expense = CreateSubmitted();

        AppException exception = Assert.ThrowsExactly<AppException>(() => expense.Reject(OwnerId, Justification, _decidedAt));

        Assert.AreEqual(StatusCodes.Status403Forbidden, exception.StatusCode);
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
        Assert.HasCount(2, expense.History);
    }

    [TestMethod]
    public void Reject_WhenDraft_ThrowsNotFound()
    {
        Expense expense = CreateDraft();

        AppException exception = Assert.ThrowsExactly<AppException>(() => expense.Reject(ApproverId, Justification, _decidedAt));

        Assert.AreEqual(StatusCodes.Status404NotFound, exception.StatusCode);
    }

    [TestMethod]
    [DataRow(ExpenseStatus.Approved)]
    [DataRow(ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Paid)]
    public void Reject_WhenAlreadyDecided_ThrowsConflictWithoutHistory(ExpenseStatus status)
    {
        Expense expense = CreateSubmitted();
        expense.Status = status;

        AppException exception = Assert.ThrowsExactly<AppException>(() => expense.Reject(ApproverId, Justification, _decidedAt));

        Assert.AreEqual(StatusCodes.Status409Conflict, exception.StatusCode);
        Assert.AreEqual(status, expense.Status);
        Assert.HasCount(2, expense.History);
    }

    [TestMethod]
    [DataRow(9)]
    [DataRow(501)]
    public void Reject_WithJustificationOutOfLimitsAfterTrim_ThrowsBadRequestAndKeepsSubmitted(int length)
    {
        Expense expense = CreateSubmitted();
        string justification = new string('a', length) + "     ";

        AppException exception = Assert.ThrowsExactly<AppException>(() => expense.Reject(ApproverId, justification, _decidedAt));

        Assert.AreEqual(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
        Assert.HasCount(2, expense.History);
    }

    [TestMethod]
    public void Approve_AfterReject_ThrowsConflict()
    {
        Expense expense = CreateSubmitted();
        expense.Reject(ApproverId, Justification, _decidedAt);

        AppException exception = Assert.ThrowsExactly<AppException>(() => expense.Approve(ApproverId, _decidedAt.AddMinutes(1)));

        Assert.AreEqual(StatusCodes.Status409Conflict, exception.StatusCode);
        Assert.AreEqual(ExpenseStatus.Rejected, expense.Status);
    }

    private static Expense CreateDraft() =>
        Expense.CreateDraft(OwnerId, "Almoço com cliente", 120.50m, new DateOnly(2026, 9, 30), 1, _createdAt);

    private static Expense CreateSubmitted()
    {
        Expense expense = CreateDraft();
        expense.Submit(OwnerId, _createdAt.AddHours(1));
        return expense;
    }
}
