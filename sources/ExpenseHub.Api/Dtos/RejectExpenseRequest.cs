using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

internal sealed class RejectExpenseRequest
{
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Justification { get; set; } = string.Empty;
}
