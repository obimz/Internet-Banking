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
public class AccountsController : ControllerBase
{
    private readonly Internet_BankingDbContext _context;

    public AccountsController(Internet_BankingDbContext context)
    {
        _context = context;
    }

    [HttpPost("create-account")]
    [Authorize]
    public async Task<IActionResult> CreateAccount(OpenAccountRequest request)
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
        await _context.SaveChangesAsync();//saves to the database

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

    [HttpGet("account-details/{accountId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetAccountDetails(Guid accountId)
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (!Guid.TryParse(customerIdValue, out var customerId))
        {
            return Unauthorized("Invalid customer ID.");
        }

        var account = await _context.BankAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId && a.CustomerId == customerId);

        if (account is null)
            return NotFound("Account not found.");

        return Ok(new
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

    [HttpGet("account-balance/{accountId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetAccountBalance(Guid accountId)
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(customerIdValue, out var customerId))
        {
            return Unauthorized("Invalid customer ID.");
        }
        
        var account = await _context.BankAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId && a.CustomerId == customerId);

        if (account is null)
            return NotFound("Account not found.");

        return Ok(new
        {
            account.Id,
            account.Balance
        });
    }

    [HttpGet("my-accounts")]
    [Authorize]
    public async Task<IActionResult> GetCustomerAccounts()
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(customerIdValue, out var customerId))
        {
            return Unauthorized("Invalid customer ID.");
        }

        var account = await _context.BankAccounts
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .Select(a => new
            {
                a.Id,
                a.AccountNumber,
                a.AccountType,
                a.Currency,
                a.Balance,
                a.Status,
                a.CreatedAt
            })
            .ToListAsync();

        return Ok(account);
    }

    [HttpGet]
    [Authorize]
    //public async Task<IActionResult> GetAllAccounts()
    //{
        
    //}

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
