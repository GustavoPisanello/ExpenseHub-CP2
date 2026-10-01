using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

internal sealed class LoginRequest
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
