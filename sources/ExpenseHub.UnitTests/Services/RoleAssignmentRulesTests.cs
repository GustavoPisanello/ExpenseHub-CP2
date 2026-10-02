using System.Collections.Generic;
using System.Linq;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

[TestClass]
internal sealed class RoleAssignmentRulesTests
{
    [TestMethod]
    public void Normalize_WithKnownRolesInAnyCase_ReturnsCanonicalNames()
    {
        IReadOnlyList<string> roles = RoleAssignmentRules.Normalize(["employee", " APPROVER "]);

        CollectionAssert.AreEqual(new List<string> { Roles.Employee, Roles.Approver }, roles.ToList());
    }

    [TestMethod]
    public void Normalize_WithDuplicatedRoles_ReturnsEachRoleOnce()
    {
        IReadOnlyList<string> roles = RoleAssignmentRules.Normalize([Roles.Finance, "finance", Roles.Finance]);

        CollectionAssert.AreEqual(new List<string> { Roles.Finance }, roles.ToList());
    }

    [TestMethod]
    public void Normalize_WithEmptyList_ReturnsNoRoles()
    {
        IReadOnlyList<string> roles = RoleAssignmentRules.Normalize([]);

        Assert.IsEmpty(roles);
    }

    [TestMethod]
    [DataRow("SuperUser")]
    [DataRow("Administrator")]
    [DataRow("")]
    public void Normalize_WithUnknownRole_ThrowsBadRequest(string unknownRole)
    {
        AppException exception = Assert.ThrowsExactly<AppException>(
            () => RoleAssignmentRules.Normalize([Roles.Employee, unknownRole]));

        Assert.AreEqual(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [TestMethod]
    public void EnsureAdminKeepsOwnRole_WhenAdminRemovesOwnAdminRole_ThrowsBadRequest()
    {
        AppException exception = Assert.ThrowsExactly<AppException>(
            () => RoleAssignmentRules.EnsureAdminKeepsOwnRole("admin-id", "admin-id", [Roles.Employee]));

        Assert.AreEqual(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [TestMethod]
    public void EnsureAdminKeepsOwnRole_WhenAdminKeepsOwnAdminRole_DoesNotThrow()
    {
        RoleAssignmentRules.EnsureAdminKeepsOwnRole("admin-id", "admin-id", [Roles.Admin, Roles.Auditor]);
    }

    [TestMethod]
    public void EnsureAdminKeepsOwnRole_WhenAdminRemovesAdminRoleFromAnotherUser_DoesNotThrow()
    {
        RoleAssignmentRules.EnsureAdminKeepsOwnRole("admin-id", "other-id", []);
    }
}
