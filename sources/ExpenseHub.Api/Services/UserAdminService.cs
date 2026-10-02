using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ExpenseHub.Api.Services;

internal sealed class UserAdminService
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public UserAdminService(AppDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<UserResponse>> ListUsersAsync(CancellationToken cancellationToken)
    {
        List<IdentityUser> users = await _context.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);

        var userRoles = await _context.UserRoles
            .Join(_context.Roles, userRole => userRole.RoleId, role => role.Id, (userRole, role) => new { userRole.UserId, role.Name })
            .ToListAsync(cancellationToken);

        return users
            .Select(user => new UserResponse(
                user.Id,
                user.Email ?? string.Empty,
                userRoles.Where(userRole => userRole.UserId == user.Id).Select(userRole => userRole.Name ?? string.Empty).Order().ToList()))
            .ToList();
    }

    public async Task<UserResponse> UpdateRolesAsync(string currentUserId, string targetUserId, IEnumerable<string> requestedRoles, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> newRoles = RoleAssignmentRules.Normalize(requestedRoles);

        IdentityUser user = await _userManager.FindByIdAsync(targetUserId)
            ?? throw AppException.NotFound("Usuário não encontrado.");

        RoleAssignmentRules.EnsureAdminKeepsOwnRole(currentUserId, targetUserId, newRoles);

        IList<string> currentRoles = await _userManager.GetRolesAsync(user);
        List<string> rolesToRemove = currentRoles.Except(newRoles).ToList();
        List<string> rolesToAdd = newRoles.Except(currentRoles).ToList();

        await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        EnsureSucceeded(await _userManager.RemoveFromRolesAsync(user, rolesToRemove));
        EnsureSucceeded(await _userManager.AddToRolesAsync(user, rolesToAdd));
        EnsureSucceeded(await _userManager.UpdateSecurityStampAsync(user));

        await transaction.CommitAsync(cancellationToken);

        return new UserResponse(user.Id, user.Email ?? string.Empty, newRoles.Order().ToList());
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw AppException.BadRequest(string.Join(" ", result.Errors.Select(error => error.Description)));
        }
    }
}
