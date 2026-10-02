using System;
using System.Linq;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Models;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

[TestClass]
internal sealed class ExpensePaymentTests
{
    private const string OwnerId = "owner-id";
    private const string ApproverId = "approver-id";
    private const string FinanceId = "finance-id";
    private const decimal Amount = 120.50m;

    private static readonly DateTime _createdAt = new(2026, 10, 1, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _paidAt = _createdAt.AddDays(1);

    [TestMethod]
    public void Pay_ApprovedByAnotherUser_MovesToPaid()
    {
        Expense expense = CreateApproved();
        Guid originalVersion = expense.Version;

        expense.Pay(FinanceId, _paidAt);

        Assert.AreEqual(ExpenseStatus.Paid, expense.Status);
        Assert.AreEqual(_paidAt, expense.UpdatedAtUtc);
        Assert.AreNotEqual(originalVersion, expense.Version);
    }

    [TestMethod]
    public void Pay_CreatesPaymentRecordWithServerActorTimeAndExpenseAmount()
    {
        Expense expense = CreateApproved();

        expense.Pay(FinanceId, _paidAt);

        Assert.IsNotNull(expense.Payment);
        Assert.AreEqual(expense.Id, expense.Payment.ExpenseId);
        Assert.AreEqual(FinanceId, expense.Payment.PaidById);
        Assert.AreEqual(_paidAt, expense.Payment.PaidAtUtc);
        Assert.AreEqual(Amount, expense.Payment.Amount);
    }

    [TestMethod]
    public void Pay_LeavesPaymentIdUnsetSoTheDatabaseInsertsIt()
    {
        Expense expense = CreateApproved();

        expense.Pay(FinanceId, _paidAt);

        Assert.IsNotNull(expense.Payment);
        Assert.AreEqual(Guid.Empty, expense.Payment.Id);
    }

    [TestMethod]
    public void Pay_RecordsPaidHistory()
    {
        Expense expense = CreateApproved();

        expense.Pay(FinanceId, _paidAt);

        ExpenseHistory history = expense.History.Last();
        Assert.AreEqual(ExpenseAction.Paid, history.Action);
        Assert.AreEqual(FinanceId, history.ActorId);
        Assert.AreEqual(_paidAt, history.OccurredAtUtc);
        Assert.AreEqual(ExpenseStatus.Approved, history.FromStatus);
        Assert.AreEqual(ExpenseStatus.Paid, history.ToStatus);
    }

    [TestMethod]
    public void Pay_WhenActorIsOwner_ThrowsForbiddenWithoutPayment()
    {
        Expense expense = CreateApproved();

        AppException exception = Assert.ThrowsExactly<AppException>(() => expense.Pay(OwnerId, _paidAt));

        Assert.AreEqual(StatusCodes.Status403Forbidden, exception.StatusCode);
        Assert.AreEqual(ExpenseStatus.Approved, expense.Status);
        Assert.IsNull(expense.Payment);
        Assert.HasCount(3, expense.History);
    }

    [TestMethod]
    public void Pay_WhenDraft_ThrowsNotFound()
    {
        Expense expense = CreateDraft();

        AppException exception = Assert.ThrowsExactly<AppException>(() => expense.Pay(FinanceId, _paidAt));

        Assert.AreEqual(StatusCodes.Status404NotFound, exception.StatusCode);
        Assert.IsNull(expense.Payment);
    }

    [TestMethod]
    [DataRow(ExpenseStatus.Submitted)]
    [DataRow(ExpenseStatus.Rejected)]
    public void Pay_WhenNotApproved_ThrowsConflictWithoutPaymentOrHistory(ExpenseStatus status)
    {
        Expense expense = CreateApproved();
        expense.Status = status;

        AppException exception = Assert.ThrowsExactly<AppException>(() => expense.Pay(FinanceId, _paidAt));

        Assert.AreEqual(StatusCodes.Status409Conflict, exception.StatusCode);
        Assert.AreEqual(status, expense.Status);
        Assert.IsNull(expense.Payment);
        Assert.HasCount(3, expense.History);
    }

    [TestMethod]
    public void Pay_Twice_ThrowsConflictAndKeepsFirstPayment()
    {
        Expense expense = CreateApproved();
        expense.Pay(FinanceId, _paidAt);

        AppException exception = Assert.ThrowsExactly<AppException>(() => expense.Pay("other-finance", _paidAt.AddMinutes(1)));

        Assert.AreEqual(StatusCodes.Status409Conflict, exception.StatusCode);
        Assert.IsNotNull(expense.Payment);
        Assert.AreEqual(FinanceId, expense.Payment.PaidById);
        Assert.AreEqual(_paidAt, expense.Payment.PaidAtUtc);
        Assert.HasCount(4, expense.History);
    }

    [TestMethod]
    public void History_AfterFullFlow_ListsEveryTransitionInOrder()
    {
        Expense expense = CreateApproved();
        expense.Pay(FinanceId, _paidAt);

        CollectionAssert.AreEqual(
            new[] { ExpenseAction.Created, ExpenseAction.Submitted, ExpenseAction.Approved, ExpenseAction.Paid },
            expense.History.OrderBy(entry => entry.OccurredAtUtc).Select(entry => entry.Action).ToArray());
        Assert.IsTrue(expense.History.All(entry => entry.ExpenseId == expense.Id));
    }

    [TestMethod]
    public void HistoryResponse_From_MapsStatusesAndJustificationAsText()
    {
        Expense expense = CreateDraft();
        expense.Submit(OwnerId, _createdAt.AddHours(1));
        expense.Reject(ApproverId, "Comprovante ilegível.", _createdAt.AddHours(2));

        ExpenseHistoryResponse created = ExpenseHistoryResponse.From(expense.History[0]);
        ExpenseHistoryResponse rejected = ExpenseHistoryResponse.From(expense.History[2]);

        Assert.IsNull(created.FromStatus);
        Assert.AreEqual("Draft", created.ToStatus);
        Assert.AreEqual("Rejected", rejected.Action);
        Assert.AreEqual("Submitted", rejected.FromStatus);
        Assert.AreEqual("Rejected", rejected.ToStatus);
        Assert.AreEqual(ApproverId, rejected.ActorId);
        Assert.AreEqual("Comprovante ilegível.", rejected.Justification);
    }

    private static Expense CreateDraft() =>
        Expense.CreateDraft(OwnerId, "Almoço com cliente", Amount, new DateOnly(2026, 9, 30), 1, _createdAt);

    private static Expense CreateApproved()
    {
        Expense expense = CreateDraft();
        expense.Submit(OwnerId, _createdAt.AddHours(1));
        expense.Approve(ApproverId, _createdAt.AddHours(2));
        return expense;
    }
}
