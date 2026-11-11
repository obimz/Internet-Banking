using Internet_Banking.Data;
using Internet_Banking.DTOs;
using Internet_Banking.DTOs.Trasactions;
using Internet_Banking.Service;
using Microsoft.EntityFrameworkCore;

namespace Internet_Banking.Services;

public class AccountStatementService : IAccountStatementService
{
    private readonly Internet_BankingDbContext _context;

    public AccountStatementService(Internet_BankingDbContext context)
    {
        _context = context;
    }

    public async Task<AccountStatementResponseDto> GetAccountStatementAsync(Guid accountId, AccountStatementFilterDto filter)
    {
        var query = _context.Transactions.AsNoTracking()
            .Where(t => t.SourceAccountId == accountId || t.DestinationAccountId == accountId);

        // Apply date filters based on the provided
        if (filter.StartDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= filter.EndDate.Value);
        }


        if (!string.IsNullOrEmpty(filter.TransactionType))
        {
            if (filter.TransactionType.Equals("Credit", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.DestinationAccountId == accountId);
            }
            else if (filter.TransactionType.Equals("Debit", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.SourceAccountId == accountId);
            }
        }

        var totalCredits = await query
            .Where(t => t.DestinationAccountId == accountId)
            .SumAsync(t => t.Amount);

        var totalDebits = await query
            .Where(t => t.SourceAccountId == accountId)
            .SumAsync(t => t.Amount);

        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TransactionStatementItemDto
            {
                TransactionReference = t.TransactionReference,
                Amount = t.Amount,
                FlowType = t.DestinationAccountId == accountId ? "Credit" : "Debit",
                Status = t.TransactionStatus.ToString(),
                Narration = t.Narration,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return new AccountStatementResponseDto
        {
            AccountId = accountId,
            TotalCredits = totalCredits,
            TotalDebits = totalDebits,
            NetChange = totalCredits - totalDebits,
            Transactions = transactions
        };
    }
}