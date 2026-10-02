using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Services;

internal sealed class ExpenseService
{
    private readonly AppDbContext _context;
    private readonly TimeProvider _timeProvider;

    public ExpenseService(AppDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<ExpenseResponse> CreateAsync(string userId, ExpenseRequest request, CancellationToken cancellationToken)
    {
        await EnsureCategoryExistsAsync(request.CategoryId.GetValueOrDefault(), cancellationToken);

        Expense expense = Expense.CreateDraft(
            userId,
            request.Description,
            request.Amount.GetValueOrDefault(),
            request.ExpenseDate.GetValueOrDefault(),
            request.CategoryId.GetValueOrDefault(),
            UtcNow());

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync(cancellationToken);

        return ExpenseResponse.From(expense);
    }

    public async Task<ExpenseResponse> UpdateAsync(string userId, Guid id, ExpenseRequest request, CancellationToken cancellationToken)
    {
        Expense expense = await _context.Expenses.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw AppException.NotFound("Reembolso não encontrado.");

        await EnsureCategoryExistsAsync(request.CategoryId.GetValueOrDefault(), cancellationToken);

        expense.UpdateDraft(
            userId,
            request.Description,
            request.Amount.GetValueOrDefault(),
            request.ExpenseDate.GetValueOrDefault(),
            request.CategoryId.GetValueOrDefault(),
            UtcNow());

        await SaveAsync(cancellationToken);

        return ExpenseResponse.From(expense);
    }

    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        if (!await _context.ExpenseCategories.AnyAsync(category => category.Id == categoryId, cancellationToken))
        {
            throw AppException.BadRequest("Categoria inexistente.");
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw AppException.Conflict("O reembolso foi alterado por outra operação. Tente novamente.");
        }
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
}
