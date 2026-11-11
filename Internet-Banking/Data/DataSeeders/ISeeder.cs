namespace Internet_Banking.Data.DataSeeders;

public interface ISeeder
{
    Task SeedAsync(Internet_BankingDbContext context);
}