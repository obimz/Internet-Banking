using Internet_Banking.Data;
using Internet_Banking.Data.DataSeeders;
using Microsoft.EntityFrameworkCore;

namespace Internet_Banking.Extensions;

public static class SeederExtension
{
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        // Create a temporary service scope to access scoped dependencies like DbContext
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Internet_BankingDbContext>();

        context.Database.SetCommandTimeout(300);//Increases the SQL timeout to 5mins for heavy seading

        // Group all seeders using the ISeeder contract in the exact order they must run
        ISeeder[] seeders =
        [
            new CustomerSeeder(),
            new BankAccountSeeder(),
            new TransactionSeeder()
        ];

        foreach (var seeder in seeders)
        {
            await seeder.SeedAsync(context);
        }
    }
}
