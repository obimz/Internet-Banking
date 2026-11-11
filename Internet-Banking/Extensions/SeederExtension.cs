using Internet_Banking.Data;
using Internet_Banking.Data.DataSeeders;

namespace Internet_Banking.Extensions;

public static class SeederExtension
{
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        // Create a temporary service scope to access scoped dependencies like DbContext
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Internet_BankingDbContext>();

        // Executes the seeders in the correct order to ensure that foreign key constraints are satisfied
        var customerSeeder = new CustomerSeeder();
        await customerSeeder.SeedAsync(context);

        var bankAccountSeeder = new BankAccountSeeder();
        await bankAccountSeeder.SeedAsync(context);

        var transactionSeeder = new TransactionSeeder();
        await transactionSeeder.SeedAsync(context);
    }
}
