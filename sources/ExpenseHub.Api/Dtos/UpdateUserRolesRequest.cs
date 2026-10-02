using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

internal sealed class UpdateUserRolesRequest
{
    [Required]
    public List<string>? Roles { get; set; }
}
