using System;
using System.Collections.Generic;
using System.Linq;
using ExpenseHub.Api.Models;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

[TestClass]
internal sealed class ExpenseTransitionMatrixTests
{
    private const string OwnerId = "owner-id";
    private const string ApproverId = "approver-id";
    private const string FinanceId = "finance-id";
    private const string Justification = "Comprovante ilegível e sem CNPJ.";
    private const string Update = nameof(Expense.UpdateDraft);
    private const string Submit = nameof(Expense.Submit);
    private const string Approve = nameof(Expense.Approve);
    private const string Reject = nameof(Expense.Reject);
    private const string Pay = nameof(Expense.Pay);
    private const int Success = 0;

    private static readonly DateTime _createdAt = new(2026, 10, 1, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _actionAt = _createdAt.AddDays(2);

    [TestMethod]
    [DataRow(ExpenseStatus.Draft, Update, Success)]
    [DataRow(ExpenseStatus.Draft, Submit, Success)]
    [DataRow(ExpenseStatus.Draft, Approve, StatusCodes.Status404NotFound)]
    [DataRow(ExpenseStatus.Draft, Reject, StatusCodes.Status404NotFound)]
    [DataRow(ExpenseStatus.Draft, Pay, StatusCodes.Status404NotFound)]
    [DataRow(ExpenseStatus.Submitted, Update, StatusCodes.Status409Conflict)]
    [DataRow(ExpenseStatus.Submitted, Submit, StatusCodes.Status409Conflict)]
    [DataRow(ExpenseStatus.Submitted, Approve, Success)]
    [DataRow(ExpenseStatus.Submitted, Reject, Success)]
    [DataRow(ExpenseStatus.Submitted, Pay, StatusCodes.Status409Conflict)]
    [DataRow(ExpenseStatus.Approved, Update, StatusCodes.Status409Conflict)]
    [DataRow(ExpenseStatus.Approved, Submit, StatusCodes.Status409Conflict)]
    [DataRow(ExpenseStatus.Approved, Approve, StatusCodes.Status409Conflict)]
    [DataRow(ExpenseStatus.Approved, Reject, StatusCodes.Status409Conflict)]
    [DataRow(ExpenseStatus.Approved, Pay, Success)]
    [DataRow(ExpenseStatus.Rejected, Update, StatusCodes.Status409Conflict)]
    [DataRow(ExpenseStatus.Rejected, Submit, StatusCodes.Status409Conflict)]
    [DataRow(ExpenseStatus.Rejected, Approve, StatusCodes.Status409Conflict)]
    [DataRow(ExpenseStatus.Rejected, Reject, StatusCodes.Status409Conflict)]
    [DataRow(ExpenseStatus.Rejected, Pay, StatusCodes.Status409Conflict)]
    [DataRow(ExpenseStatus.Paid, Update, StatusCodes.Status409Conflict)]
    [DataRow(ExpenseStatus.Paid, Submit, StatusCodes.Status409Conflict)]
    [DataRow(ExpenseStatus.Paid, Approve, StatusCodes.Status409Conflict)]
    [DataRow(ExpenseStatus.Paid, Reject, StatusCodes.Status409Conflict)]
    [DataRow(ExpenseStatus.Paid, Pay, StatusCodes.Status409Conflict)]
    public void Transition_ByLegitimateActor_FollowsTheStateTable(ExpenseStatus status, string action, int expectedStatusCode)
    {
        Expense expense = CreateIn(status);
        Guid version = expense.Version;
        int historyCount = expense.History.Count;
        PaymentRecord? payment = expense.Payment;

        if (expectedStatusCode == Success)
        {
            Run(expense, action, LegitimateActor(action));

            Assert.AreNotEqual(version, expense.Version);
            Assert.HasCount(historyCount + 1, expense.History);
            Assert.AreEqual(status, expense.History.Last().FromStatus);
            Assert.AreEqual(expense.Status, expense.History.Last().ToStatus);
            return;
        }

        AppException exception = Assert.ThrowsExactly<AppException>(() => Run(expense, action, LegitimateActor(action)));

        Assert.AreEqual(expectedStatusCode, exception.StatusCode);
        Assert.AreEqual(status, expense.Status);
        Assert.AreEqual(version, expense.Version);
        Assert.HasCount(historyCount, expense.History);
        Assert.AreSame(payment, expense.Payment);
    }

    [TestMethod]
    [DataRow(ExpenseStatus.Submitted)]
    [DataRow(ExpenseStatus.Approved)]
    [DataRow(ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Paid)]
    public void ApproveRejectAndPay_WhenActorIsOwner_ThrowForbiddenInEveryVisibleStatus(ExpenseStatus status)
    {
        foreach (string action in new[] { Approve, Reject, Pay })
        {
            Expense expense = CreateIn(status);
            int historyCount = expense.History.Count;

            AppException exception = Assert.ThrowsExactly<AppException>(() => Run(expense, action, OwnerId), action);

            Assert.AreEqual(StatusCodes.Status403Forbidden, exception.StatusCode, action);
            Assert.AreEqual(status, expense.Status, action);
            Assert.HasCount(historyCount, expense.History, action);
        }
    }

    [TestMethod]
    [DataRow(ExpenseStatus.Draft)]
    [DataRow(ExpenseStatus.Submitted)]
    [DataRow(ExpenseStatus.Approved)]
    [DataRow(ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Paid)]
    public void UpdateAndSubmit_WhenActorIsNotOwner_ThrowNotFoundInEveryStatus(ExpenseStatus status)
    {
        foreach (string action in new[] { Update, Submit })
        {
            Expense expense = CreateIn(status);
            int historyCount = expense.History.Count;

            AppException exception = Assert.ThrowsExactly<AppException>(() => Run(expense, action, ApproverId), action);

            Assert.AreEqual(StatusCodes.Status404NotFound, exception.StatusCode, action);
            Assert.AreEqual(status, expense.Status, action);
            Assert.HasCount(historyCount, expense.History, action);
        }
    }

    [TestMethod]
    public void History_AlongTheApprovedFlow_ChainsStatusesAndKeepsEachActor()
    {
        Expense expense = CreateIn(ExpenseStatus.Paid);

        List<ExpenseHistory> history = expense.History;

        CollectionAssert.AreEqual(
            new[] { OwnerId, OwnerId, ApproverId, FinanceId },
            history.Select(entry => entry.ActorId).ToArray());
        Assert.IsNull(history[0].FromStatus);
        for (int index = 1; index < history.Count; index++)
        {
            Assert.AreEqual(history[index - 1].ToStatus, history[index].FromStatus);
            Assert.IsGreaterThan(history[index - 1].OccurredAtUtc, history[index].OccurredAtUtc);
        }

        Assert.IsTrue(history.All(entry => entry.OccurredAtUtc.Kind == DateTimeKind.Utc));
        Assert.IsTrue(history.All(entry => entry.Justification is null));
    }

    [TestMethod]
    public void History_OnlyRejectionCarriesJustification()
    {
        Expense expense = CreateIn(ExpenseStatus.Rejected);

        List<ExpenseHistory> withJustification = expense.History.Where(entry => entry.Justification is not null).ToList();

        Assert.HasCount(1, withJustification);
        Assert.AreEqual(ExpenseAction.Rejected, withJustification[0].Action);
        Assert.AreEqual(Justification, withJustification[0].Justification);
    }

    [TestMethod]
    public void Payment_ExistsOnlyAfterPay()
    {
        foreach (ExpenseStatus status in Enum.GetValues<ExpenseStatus>())
        {
            Assert.AreEqual(status == ExpenseStatus.Paid, CreateIn(status).Payment is not null, status.ToString());
        }
    }

    private static string LegitimateActor(string action) => action switch
    {
        Update or Submit => OwnerId,
        Pay => FinanceId,
        _ => ApproverId,
    };

    private static void Run(Expense expense, string action, string actorId)
    {
        switch (action)
        {
            case Update:
                expense.UpdateDraft(actorId, "Descrição alterada no teste", 99.90m, new DateOnly(2026, 9, 29), 2, _actionAt);
                break;
            case Submit:
                expense.Submit(actorId, _actionAt);
                break;
            case Approve:
                expense.Approve(actorId, _actionAt);
                break;
            case Reject:
                expense.Reject(actorId, Justification, _actionAt);
                break;
            default:
                expense.Pay(actorId, _actionAt);
                break;
        }
    }

    // Monta o estado pelo fluxo real, sem atribuir Status direto.
    private static Expense CreateIn(ExpenseStatus status)
    {
        Expense expense = Expense.CreateDraft(OwnerId, "Almoço com cliente", 120.50m, new DateOnly(2026, 9, 30), 1, _createdAt);
        if (status == ExpenseStatus.Draft)
        {
            return expense;
        }

        expense.Submit(OwnerId, _createdAt.AddHours(1));
        if (status == ExpenseStatus.Submitted)
        {
            return expense;
        }

        if (status == ExpenseStatus.Rejected)
        {
            expense.Reject(ApproverId, Justification, _createdAt.AddHours(2));
            return expense;
        }

        expense.Approve(ApproverId, _createdAt.AddHours(2));
        if (status == ExpenseStatus.Paid)
        {
            expense.Pay(FinanceId, _createdAt.AddHours(3));
        }

        return expense;
    }
}
