using Bogus;
using Internet_Banking.Models;
using Microsoft.EntityFrameworkCore;

namespace Internet_Banking.Data.DataSeeders;

public class BankAccountSeeder : ISeeder
{
    public async Task SeedAsync(Internet_BankingDbContext context)
    {
        if (await context.BankAccounts.AnyAsync())
            return;

        var customerIds = await context.Customers //fetches customerIds for the foreign key relationship.
            .AsNoTracking()
            .Select(c => c.Id)
            .ToListAsync();

        if (!customerIds.Any())
            return;

        var accountFaker = new Faker<BankAccount>()
            .RuleFor(a => a.Id, f => Guid.NewGuid())
            .RuleFor(a => a.AccountNumber, f => f.Random.Replace("##########"))
            .RuleFor(a => a.Status, f => f.PickRandom<AccountStatus>())
            .RuleFor(a => a.Currency, _ => "NGN")
            .RuleFor(a => a.Balance, f => Math.Round(f.Random.Decimal(1000m, 5000000m), 2))
            .RuleFor(a => a.DailyTransferLimit, f => Math.Round(f.Random.Decimal(50000m, 10000000m), 2))
            .RuleFor(a => a.CreatedAt, f => f.Date.Past(1));

        var accounts = new List<BankAccount>(customerIds.Count * 2);//pre-allocates memeory for 2000 so the list never resizes while looping

        foreach (var customerId in customerIds)
        {
            var customerAccounts = accountFaker.Generate(2);//generate 2 account for the current customer

            customerAccounts[0].CustomerId = customerId;//assigns the first account as savings
            customerAccounts[0].AccountType = AccountType.Savings;

            customerAccounts[1].CustomerId = customerId;//assigns the second account as current
            customerAccounts[1].AccountType = AccountType.Current;

            accounts.AddRange(customerAccounts);//saves the both account to the main list
        }

        await context.BankAccounts.AddRangeAsync(accounts);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();//Frees up memory in DBcontext before the TransactionSeeder runs
    }
}