using System;
using System.Collections.Generic;
using System.Linq;
using ExpenseHub.Api.Models;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

[TestClass]
internal sealed class ExpenseAccessPolicyTests
{
    private const string Ana = "ana";
    private const string Bruno = "bruno";

    private static readonly List<Expense> _expenses =
    [
        NewExpense("ana-draft", Ana, ExpenseStatus.Draft),
        NewExpense("ana-submitted", Ana, ExpenseStatus.Submitted),
        NewExpense("ana-paid", Ana, ExpenseStatus.Paid),
        NewExpense("bruno-draft", Bruno, ExpenseStatus.Draft),
        NewExpense("bruno-submitted", Bruno, ExpenseStatus.Submitted),
        NewExpense("bruno-approved", Bruno, ExpenseStatus.Approved),
        NewExpense("bruno-rejected", Bruno, ExpenseStatus.Rejected),
    ];

    [TestMethod]
    public void VisibleTo_Employee_SeesOnlyOwnExpensesInAnyStatus()
    {
        CollectionAssert.AreEquivalent(
            new List<string> { "ana-draft", "ana-submitted", "ana-paid" },
            Visible(Ana, Roles.Employee));
    }

    [TestMethod]
    public void VisibleTo_Employee_DoesNotSeeAnotherEmployeeExpenses()
    {
        List<string> visible = Visible(Bruno, Roles.Employee);

        Assert.IsFalse(visible.Any(description => description.StartsWith(Ana, StringComparison.Ordinal)));
    }

    [TestMethod]
    public void VisibleTo_Approver_SeesOnlySubmitted()
    {
        CollectionAssert.AreEquivalent(
            new List<string> { "ana-submitted", "bruno-submitted" },
            Visible("carla", Roles.Approver));
    }

    [TestMethod]
    public void VisibleTo_Finance_SeesOnlyApprovedAndPaid()
    {
        CollectionAssert.AreEquivalent(
            new List<string> { "ana-paid", "bruno-approved" },
            Visible("fabio", Roles.Finance));
    }

    [TestMethod]
    public void VisibleTo_Auditor_SeesEverything()
    {
        Assert.HasCount(_expenses.Count, Visible("duda", Roles.Auditor));
    }

    [TestMethod]
    public void VisibleTo_EmployeeAndApprover_SeesOwnExpensesPlusAllSubmitted()
    {
        CollectionAssert.AreEquivalent(
            new List<string> { "ana-draft", "ana-submitted", "ana-paid", "bruno-submitted" },
            Visible(Ana, Roles.Employee, Roles.Approver));
    }

    [TestMethod]
    public void VisibleTo_AdminWithoutFunctionalRole_SeesNothing()
    {
        Assert.IsEmpty(Visible("admin", Roles.Admin));
    }

    [TestMethod]
    public void VisibleTo_UserWithoutRoles_SeesNothing()
    {
        Assert.IsEmpty(Visible(Ana));
    }

    [TestMethod]
    public void VisibleTo_ApproverWithoutEmployee_DoesNotSeeOwnDraft()
    {
        CollectionAssert.DoesNotContain(Visible(Ana, Roles.Approver), "ana-draft");
    }

    [TestMethod]
    public void VisibleTo_Approver_DoesNotSeeDraftApprovedRejectedOrPaid()
    {
        List<string> visible = Visible("carla", Roles.Approver);

        CollectionAssert.DoesNotContain(visible, "bruno-draft");
        CollectionAssert.DoesNotContain(visible, "bruno-approved");
        CollectionAssert.DoesNotContain(visible, "bruno-rejected");
        CollectionAssert.DoesNotContain(visible, "ana-paid");
    }

    [TestMethod]
    public void VisibleTo_Finance_DoesNotSeeDraftSubmittedOrRejected()
    {
        List<string> visible = Visible("fabio", Roles.Finance);

        CollectionAssert.DoesNotContain(visible, "ana-draft");
        CollectionAssert.DoesNotContain(visible, "bruno-submitted");
        CollectionAssert.DoesNotContain(visible, "bruno-rejected");
    }

    [TestMethod]
    public void VisibleTo_EmployeeAndFinance_SeesOwnExpensesPlusApprovedAndPaid()
    {
        CollectionAssert.AreEquivalent(
            new List<string> { "bruno-draft", "bruno-submitted", "bruno-approved", "bruno-rejected", "ana-paid" },
            Visible(Bruno, Roles.Employee, Roles.Finance));
    }

    [TestMethod]
    public void VisibleTo_AdminAndEmployee_SeesOnlyOwnExpenses()
    {
        CollectionAssert.AreEquivalent(
            new List<string> { "ana-draft", "ana-submitted", "ana-paid" },
            Visible(Ana, Roles.Admin, Roles.Employee));
    }

    [TestMethod]
    public void VisibleTo_AllFunctionalRolesExceptAuditor_StillHidesOtherUsersDraftAndRejected()
    {
        List<string> visible = Visible(Ana, Roles.Employee, Roles.Approver, Roles.Finance);

        CollectionAssert.DoesNotContain(visible, "bruno-draft");
        CollectionAssert.DoesNotContain(visible, "bruno-rejected");
    }

    [TestMethod]
    public void OwnedBy_ReturnsOnlyExpensesOfThatUserInAnyStatus()
    {
        CollectionAssert.AreEquivalent(
            new List<string> { "bruno-draft", "bruno-submitted", "bruno-approved", "bruno-rejected" },
            Owned(Bruno));
    }

    [TestMethod]
    public void OwnedBy_UserWithoutExpenses_ReturnsNothing()
    {
        Assert.IsEmpty(Owned("carla"));
    }

    private static List<string> Owned(string userId)
    {
        Func<Expense, bool> filter = ExpenseAccessPolicy.OwnedBy(userId).Compile();
        return _expenses.Where(filter).Select(expense => expense.Description).ToList();
    }

    private static List<string> Visible(string userId, params string[] roles)
    {
        Func<Expense, bool> filter = ExpenseAccessPolicy.VisibleTo(userId, roles).Compile();
        return _expenses.Where(filter).Select(expense => expense.Description).ToList();
    }

    private static Expense NewExpense(string description, string ownerId, ExpenseStatus status) => new()
    {
        Id = Guid.NewGuid(),
        Description = description,
        OwnerId = ownerId,
        Status = status,
    };
}
