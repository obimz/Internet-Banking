using Internet_Banking.Data;
using Internet_Banking.Models;
using Microsoft.EntityFrameworkCore;

namespace Internet_Banking.Services;

public class AccountService : IAccountService
{
    private readonly Internet_BankingDbContext _context;

    public AccountService(Internet_BankingDbContext context)
    {
        _context = context;
    }

    public async Task<BankAccount> CreateAccountAsync(Guid customerId, AccountType accountType, decimal initialDeposit)
    {
        var accountNumber = await GenerateUniqueAccountNumberAsync();
        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            AccountType = accountType,
            Currency = "NGN",
            Balance = initialDeposit,
            Status = AccountStatus.Active,
            AccountNumber = accountNumber,
            DailyTransferLimit = 200000m,
            CreatedAt = DateTime.UtcNow
        };
        _context.BankAccounts.Add(account);//Adds the amount to the EF core memory tracker.

        return account;
    }

    public async Task<string> GenerateUniqueAccountNumberAsync()
    {
        string accountNumber;
        bool exists;
        do
        {
            accountNumber = Random.Shared.NextInt64(1000000000L, 9999999999L).ToString();
            exists = await _context.BankAccounts.AnyAsync(a => a.AccountNumber == accountNumber);
        } while (exists);
        return accountNumber;
    }
}
