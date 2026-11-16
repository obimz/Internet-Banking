using Internet_Banking.Data;
using Internet_Banking.DTOs.Trasactions;
using Internet_Banking.Models;
using Microsoft.EntityFrameworkCore;

namespace Internet_Banking.Services;

public class TransactionService : ITransactionService
{
    private readonly Internet_BankingDbContext _context;

    public TransactionService(Internet_BankingDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult> ExecuteInternalTransferAsync(
        Guid customerId,
        Guid sourceAccountId,
        Guid destinationAccountId,
        decimal amount,
        string? narration)
    {
        if (amount <= 0)
        {
            return new ServiceResult(false, 400, "Transfer amount must be greater than zero.");
        }

        if (sourceAccountId == destinationAccountId)
        {
            return new ServiceResult(false, 400, "Source and destination accounts cannot be the same.");
        }

        var sourceAccount = await _context.BankAccounts
            .FirstOrDefaultAsync(a => a.CustomerId == customerId && a.Id == sourceAccountId);

        if (sourceAccount is null)
        {
            return new ServiceResult(false, 404, "Source account not found.");
        }

        var destinationAccount = await _context.BankAccounts
            .FirstOrDefaultAsync(a => a.Id == destinationAccountId);

        if (destinationAccount is null)
        {
            return new ServiceResult(false, 404, "Destination account not found.");
        }

        if (sourceAccount.Status != AccountStatus.Active || destinationAccount.Status != AccountStatus.Active)
        {
            return new ServiceResult(false, 400, "Both accounts must be active.");
        }

        if (sourceAccount.Balance < amount)
        {
            return new ServiceResult(false, 400, "Insufficient funds in source account.");
        }

        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        sourceAccount.Balance -= amount;
        destinationAccount.Balance += amount;

        var transactionReference = GenerateTransactionReference();

        var transaction = new Transaction
        {
            TransactionReference = transactionReference,
            TransactionType = TransactionType.InternalTransfer,
            SourceAccountId = sourceAccount.Id,
            DestinationAccountId = destinationAccount.Id,
            Amount = amount,
            TransactionStatus = TransactionStatus.Successful,
            Narration = string.IsNullOrWhiteSpace(narration) ? "Internal transfer" : narration.Trim()
        };

        _context.Transactions.Add(transaction);

        await _context.SaveChangesAsync();
        await dbTransaction.CommitAsync();

        return new ServiceResult(true, 200, Data: new
        {
            transactionReference,
            sourceAccountId = sourceAccount.Id,
            sourceAccountBalance = sourceAccount.Balance,
            destinationAccountId = destinationAccount.Id
        });
    }

    private static string GenerateTransactionReference()
    {
        return $"TRX-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }
}