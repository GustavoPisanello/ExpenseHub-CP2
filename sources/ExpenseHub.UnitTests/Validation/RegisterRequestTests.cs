using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ExpenseHub.Api.Dtos;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Validation;

[TestClass]
internal sealed class RegisterRequestTests
{
    private const string ValidCredential = "Abc#12345";

    [TestMethod]
    public void RegisterRequest_DoesNotExposeRoleProperty()
    {
        Assert.IsNull(typeof(RegisterRequest).GetProperty("Role"));
        Assert.IsNull(typeof(RegisterRequest).GetProperty("Roles"));
    }

    [TestMethod]
    public void Validate_WithValidEmailAndPassword_HasNoErrors()
    {
        List<string> invalidFields = Validate(new RegisterRequest { Email = "ana@empresa.com", Password = ValidCredential });

        Assert.IsEmpty(invalidFields);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("nao-e-email")]
    public void Validate_WithInvalidEmail_ReturnsEmailError(string email)
    {
        List<string> invalidFields = Validate(new RegisterRequest { Email = email, Password = ValidCredential });

        CollectionAssert.AreEqual(new List<string> { nameof(RegisterRequest.Email) }, invalidFields);
    }

    [TestMethod]
    public void Validate_WithoutPassword_ReturnsPasswordError()
    {
        List<string> invalidFields = Validate(new RegisterRequest { Email = "ana@empresa.com", Password = string.Empty });

        CollectionAssert.AreEqual(new List<string> { nameof(RegisterRequest.Password) }, invalidFields);
    }

    private static List<string> Validate(RegisterRequest request)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results.SelectMany(result => result.MemberNames).Distinct().ToList();
    }
}
