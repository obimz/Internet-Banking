using Bogus;
using Microsoft.EntityFrameworkCore;
using Internet_Banking.Models;
using Microsoft.AspNetCore.Identity;

namespace Internet_Banking.Data.DataSeeders;

public class CustomerSeeder : ISeeder
{
    public async Task SeedAsync(Internet_BankingDbContext context)
    {
        if (await context.Customers.AnyAsync())
        {
            return; // Exit if customers already exist
        }

        var passwordHasher = new PasswordHasher<Customer>();

        var customerFaker = new Faker<Customer>()
            .RuleFor(c => c.Id, f => Guid.NewGuid())
            .RuleFor(c => c.FullName, f => f.Name.FirstName())
            .RuleFor(c => c.Email, (f, c) => f.Internet.Email(c.FullName))
            .RuleFor(c => c.PhoneNumber, f => f.Phone.PhoneNumber("+234 80# ### ####"))
            .RuleFor(c => c.PasswordHash, (f,c) => passwordHasher.HashPassword(c, "Obimba1234!")) // Hash the password Obimba1234!
            .RuleFor(c => c.CreatedAt, f => f.Date.Past(1));

        var customers = customerFaker.Generate(30);
        await context.Customers.AddRangeAsync(customers);
        await context.SaveChangesAsync();
    }
}

