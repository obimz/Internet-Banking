using Bogus;
using Internet_Banking.Models;
using Microsoft.EntityFrameworkCore;

namespace Internet_Banking.Data.DataSeeders;

public class BankAccountSeeder : ISeeder
{
    public async Task SeedAsync(Internet_BankingDbContext context)
    {
        if (await context.BankAccounts.AnyAsync())
        {
            return; // Exit if bank accounts already exist
        }

        var customerIds = await context.Customers.Select(c => c.Id).ToListAsync();//fetches all customer IDs from the database

        if (!customerIds.Any())
            return;

        var accountFaker = new Faker<BankAccount>()
            .RuleFor(a => a.Id, f => Guid.NewGuid())
            .RuleFor(a => a.AccountNumber, f => f.Random.Replace("##########"))
            .RuleFor(a => a.CustomerId, f => f.PickRandom(customerIds))
            .RuleFor(a => a.AccountType, f => f.PickRandom<AccountType>())
            .RuleFor(a => a.Status, f => f.PickRandom<AccountStatus>())
            .RuleFor(a => a.Currency, f => "NGN")
            .RuleFor(a => a.Balance, f => Math.Round(f.Random.Decimal(1000m, 5000000m), 2))
            .RuleFor(a => a.DailyTransferLimit, f => Math.Round(f.Random.Decimal(50000m, 10000000m), 2))
            .RuleFor(a => a.CreatedAt, f => f.Date.Past(1));

        var accounts = accountFaker.Generate(20);

        await context.BankAccounts.AddRangeAsync(accounts);
        await context.SaveChangesAsync();
    }
}

