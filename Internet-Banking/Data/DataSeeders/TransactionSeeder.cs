using Bogus;
using Internet_Banking.Models;
using Microsoft.EntityFrameworkCore;

namespace Internet_Banking.Data.DataSeeders;

public class TransactionSeeder : ISeeder
{
    public async Task SeedAsync(Internet_BankingDbContext context)
    {
        if (await context.Transactions.AnyAsync())
            return;

        // Fetch only the Account IDs without EF change tracking
        var accountIds = await context.BankAccounts
            .AsNoTracking()
            .Select(a => a.Id)
            .ToListAsync();

        if (accountIds.Count < 2)
            return;

        // Holds the current account ID being processed in the loop below
        Guid currentAccountId = Guid.Empty;

        var transactionFaker = new Faker<Transaction>()
            .RuleFor(t => t.Id, _ => Guid.NewGuid())
            .RuleFor(t => t.TransactionType, f => f.PickRandom<TransactionType>())
            .RuleFor(t => t.Amount, f => Math.Round(f.Random.Decimal(100m, 500000m), 2))
            .RuleFor(t => t.TransactionStatus, f => f.PickRandom<TransactionStatus>())
            .RuleFor(t => t.Narration, f => f.Lorem.Sentence(3))
            .RuleFor(t => t.CreatedAt, f => f.Date.Past(1))
            .RuleFor(t => t.TransactionReference, (f, t) =>
                $"TRX-{t.CreatedAt:yyyyMMdd}-{f.Random.AlphaNumeric(8).ToUpperInvariant()}")
            .FinishWith((f, t) =>
            {
                switch (t.TransactionType)
                {
                    case TransactionType.Deposit:
                        t.SourceAccountId = null;
                        t.DestinationAccountId = currentAccountId;
                        break;

                    case TransactionType.Withdrawal:
                        t.SourceAccountId = currentAccountId;
                        t.DestinationAccountId = null;
                        break;

                    case TransactionType.InternalTransfer:
                    case TransactionType.DomesticTransfer:
                        t.SourceAccountId = currentAccountId;

                        // Pick a random destination account that isn't the source account
                        Guid destinationId;
                        do
                        {
                            destinationId = f.PickRandom(accountIds);
                        } while (destinationId == currentAccountId);

                        t.DestinationAccountId = destinationId;
                        break;
                }
            });

        // Turn off change detection so inserting 400,000 rows doesn't slow down EF Core
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            var batch = new List<Transaction>(10000);

            foreach (var accountId in accountIds)
            {
                currentAccountId = accountId;

                // Generate 200 transactions for the current account
                var accountTransactions = transactionFaker.Generate(200);
                batch.AddRange(accountTransactions);

                // Save to database every 10,000 rows (50 accounts) and free memory
                if (batch.Count >= 10000)
                {
                    await context.Transactions.AddRangeAsync(batch);
                    await context.SaveChangesAsync();
                    context.ChangeTracker.Clear();
                    batch.Clear();
                }
            }

            // Save any remaining transactions
            if (batch.Count > 0)
            {
                await context.Transactions.AddRangeAsync(batch);
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();
            }
        }
        finally
        {
            context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }
}