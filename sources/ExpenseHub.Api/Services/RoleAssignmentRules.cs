using System;
using System.Collections.Generic;
using System.Linq;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Services;

internal static class RoleAssignmentRules
{
    public static IReadOnlyList<string> Normalize(IEnumerable<string> requestedRoles)
    {
        List<string> roles = [];

        foreach (string requested in requestedRoles)
        {
            string? known = Roles.All.FirstOrDefault(role => string.Equals(role, requested?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (known is null)
            {
                throw AppException.BadRequest($"Role desconhecida: '{requested}'.");
            }

            if (!roles.Contains(known))
            {
                roles.Add(known);
            }
        }

        return roles;
    }

    public static void EnsureAdminKeepsOwnRole(string currentUserId, string targetUserId, IReadOnlyList<string> newRoles)
    {
        if (currentUserId == targetUserId && !newRoles.Contains(Roles.Admin))
        {
            throw AppException.BadRequest("O Admin não pode remover a própria role Admin.");
        }
    }
}
