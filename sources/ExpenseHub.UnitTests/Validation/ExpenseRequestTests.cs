using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using ExpenseHub.Api.Dtos;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Validation;

[TestClass]
internal sealed class ExpenseRequestTests
{
    [TestMethod]
    public void ExpenseRequest_DoesNotExposeServerControlledFields()
    {
        string[] forbidden = ["Id", "OwnerId", "Status", "ActorId", "CreatedAtUtc", "UpdatedAtUtc", "Version"];

        foreach (string property in forbidden)
        {
            Assert.IsNull(typeof(ExpenseRequest).GetProperty(property), property);
        }
    }

    [TestMethod]
    public void Validate_WithValidRequest_HasNoErrors()
    {
        Assert.IsEmpty(Validate(ValidRequest()));
    }

    [TestMethod]
    [DataRow("0.01")]
    [DataRow("2147483647")]
    public void Validate_WithAmountAtLimits_HasNoErrors(string amount)
    {
        ExpenseRequest request = ValidRequest();
        request.Amount = decimal.Parse(amount, CultureInfo.InvariantCulture);

        Assert.IsEmpty(Validate(request));
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("0.009")]
    [DataRow("-10")]
    [DataRow("2147483647.01")]
    public void Validate_WithAmountOutOfRange_ReturnsAmountError(string amount)
    {
        ExpenseRequest request = ValidRequest();
        request.Amount = decimal.Parse(amount, CultureInfo.InvariantCulture);

        CollectionAssert.AreEqual(new List<string> { nameof(ExpenseRequest.Amount) }, Validate(request));
    }

    [TestMethod]
    [DataRow(10)]
    [DataRow(500)]
    public void Validate_WithDescriptionAtLimits_HasNoErrors(int length)
    {
        ExpenseRequest request = ValidRequest();
        request.Description = new string('a', length);

        Assert.IsEmpty(Validate(request));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(9)]
    [DataRow(501)]
    public void Validate_WithDescriptionOutOfRange_ReturnsDescriptionError(int length)
    {
        ExpenseRequest request = ValidRequest();
        request.Description = new string('a', length);

        CollectionAssert.AreEqual(new List<string> { nameof(ExpenseRequest.Description) }, Validate(request));
    }

    [TestMethod]
    public void Validate_WithoutRequiredFields_ReturnsEachMissingField()
    {
        ExpenseRequest request = new() { Description = "Almoço com cliente" };

        CollectionAssert.AreEquivalent(
            new List<string> { nameof(ExpenseRequest.Amount), nameof(ExpenseRequest.ExpenseDate), nameof(ExpenseRequest.CategoryId) },
            Validate(request));
    }

    private static ExpenseRequest ValidRequest() => new()
    {
        Description = "Almoço com cliente",
        Amount = 120.50m,
        ExpenseDate = new DateOnly(2026, 9, 30),
        CategoryId = 1,
    };

    private static List<string> Validate(ExpenseRequest request)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results.SelectMany(result => result.MemberNames).Distinct().ToList();
    }
}
