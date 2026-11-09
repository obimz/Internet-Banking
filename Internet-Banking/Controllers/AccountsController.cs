using Internet_Banking.Data;
using Internet_Banking.DTOs.Accounts;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Internet_Banking.Models;
using Microsoft.AspNetCore.Authorization;

namespace Internet_Banking.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountsController : Controller
{
    private readonly Internet_BankingDbContext _context;

    public AccountsController(Internet_BankingDbContext context)
    {
        _context = context;
    }

    [HttpPost("OpenAccount")]
    [Authorize]
    public async Task<IActionResult> OpenAccount(OpenAccountRequest request)
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(customerIdValue, out var customerId))
        {
            return Unauthorized("Invalid customer ID.");
        }

        var customerExists = await _context.Customers.AnyAsync(c => c.Id == customerId);

        if (!customerExists)
        {
            return NotFound("Customer not found.");
        }

        var accountNumber = await GenerateUniqueAccountNumberAsync();

        var account = new BankAccount
        {
            CustomerId = customerId,
            AccountType = request.AccountType,
            Currency = "NGN",
            Balance = request.InitialDeposit,
            Status = AccountStatus.Active,
            AccountNumber = accountNumber,
            CreatedAt = DateTime.UtcNow
        };

        _context.BankAccounts.Add(account);
        await _context.SaveChangesAsync();

        return Created($"/api/accounts/{account.Id}", new
        {
            account.Id,
            account.AccountNumber,
            account.AccountType,
            account.Currency,
            account.Balance,
            account.Status,
            account.CreatedAt
        });
    }


    private async Task<string> GenerateUniqueAccountNumberAsync()
    {
        while (true)
        {
            var accountNumber = Random.Shared.NextInt64(1000000000, 9999999999).ToString();

            var exists = await _context.BankAccounts
                .AnyAsync(a => a.AccountNumber == accountNumber);

            if (!exists)
            {
                return accountNumber;
            }
        }
    }
}
