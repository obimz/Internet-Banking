using Bogus;
using Internet_Banking.Models;
using Microsoft.EntityFrameworkCore;

namespace Internet_Banking.Data.DataSeeders;

public class TransactionSeeder : ISeeder
{
    public async Task SeedAsync(Internet_BankingDbContext context)
    {
        if (await context.Transactions.AnyAsync())
        {
            return; // Exit if transactions already exist
        }

        var accountIds = await context.BankAccounts.Select(a => a.Id).ToListAsync();


        // Verifies that there are at least two bank accounts in the database before proceeding with seeding transactions
        if (accountIds.Count < 2)
            return;

        var transactionFaker = new Faker<Transaction>()
            .RuleFor(t => t.Id, f => Guid.NewGuid())
            .RuleFor(t => t.TransactionType, f => f.PickRandom<TransactionType>())
            .RuleFor(t => t.Amount, f => Math.Round(f.Random.Decimal(100m, 500000m), 2))
            .RuleFor(t => t.TransactionStatus, f => f.PickRandom<TransactionStatus>())
            .RuleFor(t => t.Narration, f => f.Lorem.Sentence(3))
            .RuleFor(t => t.CreatedAt, f => f.Date.Past(1))
            .RuleFor(t => t.TransactionReference, (f, t) =>
                $"TRX-{t.CreatedAt:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant()}")
            
            .FinishWith((f, t) =>
            {
                switch (t.TransactionType)
                {
                    case TransactionType.Deposit: // Ensures that for deposit transactions, the source account is null and a destination account is randomly selected from the available accounts
                        t.SourceAccountId = null;
                        t.DestinationAccountId = f.PickRandom(accountIds);
                        break;

                    case TransactionType.Withdrawal:
                        t.SourceAccountId = f.PickRandom(accountIds);
                        t.DestinationAccountId = null;
                        break;

                    case TransactionType.InternalTransfer:
                    case TransactionType.DomesticTransfer:
                        t.SourceAccountId = f.PickRandom(accountIds);
                        var otherAccounts = accountIds.Where(id => id != t.SourceAccountId).ToList();
                        t.DestinationAccountId = f.PickRandom(otherAccounts);
                        break;
                }
            });

        var transactions = transactionFaker.Generate(50);

        await context.Transactions.AddRangeAsync(transactions);
        await context.SaveChangesAsync();


    }
}