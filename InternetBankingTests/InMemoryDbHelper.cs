using Internet_Banking.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;


namespace InternetBankingTests;

public static class InMemoryDbHelper
{
    public static Internet_BankingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Internet_BankingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Internet_BankingDbContext(options);
    }

}