using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ExpenseHub.Api.Models;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Services;

internal static class ExpenseAccessPolicy
{
    public static Expression<Func<Expense, bool>> VisibleTo(string userId, IReadOnlyCollection<string> roles)
    {
        bool isEmployee = roles.Contains(Roles.Employee);
        bool isApprover = roles.Contains(Roles.Approver);
        bool isFinance = roles.Contains(Roles.Finance);
        bool isAuditor = roles.Contains(Roles.Auditor);

        return expense =>
            isAuditor
            || (isEmployee && expense.OwnerId == userId)
            || (isApprover && expense.Status == ExpenseStatus.Submitted)
            || (isFinance && (expense.Status == ExpenseStatus.Approved || expense.Status == ExpenseStatus.Paid));
    }
}
