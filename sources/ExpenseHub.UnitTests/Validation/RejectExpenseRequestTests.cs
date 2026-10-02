using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ExpenseHub.Api.Dtos;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Validation;

[TestClass]
internal sealed class RejectExpenseRequestTests
{
    [TestMethod]
    public void RejectExpenseRequest_ExposesOnlyJustification()
    {
        List<string> properties = typeof(RejectExpenseRequest).GetProperties().Select(property => property.Name).ToList();

        CollectionAssert.AreEqual(new List<string> { nameof(RejectExpenseRequest.Justification) }, properties);
    }

    [TestMethod]
    [DataRow(10)]
    [DataRow(500)]
    public void Validate_WithJustificationAtLimits_HasNoErrors(int length)
    {
        Assert.IsEmpty(Validate(new string('a', length)));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(9)]
    [DataRow(501)]
    public void Validate_WithJustificationOutOfLimits_ReturnsJustificationError(int length)
    {
        CollectionAssert.AreEqual(
            new List<string> { nameof(RejectExpenseRequest.Justification) },
            Validate(new string('a', length)));
    }

    [TestMethod]
    public void Validate_WithBlankJustification_ReturnsJustificationError()
    {
        CollectionAssert.AreEqual(
            new List<string> { nameof(RejectExpenseRequest.Justification) },
            Validate(new string(' ', 20)));
    }

    private static List<string> Validate(string justification)
    {
        RejectExpenseRequest request = new() { Justification = justification };
        List<ValidationResult> results = [];
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results.SelectMany(result => result.MemberNames).Distinct().ToList();
    }
}
