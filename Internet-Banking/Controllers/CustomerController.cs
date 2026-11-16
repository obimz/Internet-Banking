using System.Security.Claims;
using Internet_Banking.Data;
using Internet_Banking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Internet_Banking.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomerController : ControllerBase
{
    private readonly Internet_BankingDbContext _context;

    public CustomerController(Internet_BankingDbContext context)
    {
        _context = context;
    }

    [HttpGet("all-customer-details")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllCustomers()
    {
        var cutomers = await _context.Customers
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.FullName,
                c.Email,
                c.PhoneNumber,
                c.CreatedAt
            })
            .ToListAsync();

        return Ok(cutomers);
    }

    [HttpGet("specific-customer-details/{customerId:guid}")]
    //[Authorize]
    public async Task<IActionResult> GetCustomerAccount(Guid customerId)
    {
        var loggedInUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(loggedInUserIdValue, out var loggedInUserId))
        {
            return Unauthorized();
        }

        if(loggedInUserId != customerId)
        {
            return Forbid();
        }

        var customer = await _context.Customers
            .AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => new
            {
                c.FullName,
                c.Email,
                c.PhoneNumber,
                c.CreatedAt,
                Account = c.BankAccounts.Select(a => new
                {
                    a.AccountNumber,
                    a.AccountType,
                    a.Currency,
                    a.Balance,
                    a.Status,
                    a.DailyTransferLimit,
                    a.CreatedAt
                })
            })
            .FirstOrDefaultAsync();

        if (customer is null)
        {
            return NotFound();
        }

        return Ok(customer);
    }

    [HttpPut("update-specific-customer-details/{customerId:guid}")]
    //[Authorize]
    public async Task<IActionResult> UpdateCustomerAccount(
        Guid customerId, UpdateCustomerRequest request)
    {
        var loggedInUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(loggedInUserIdValue, out var loggedInUserId))
        {
            return Unauthorized();
        }

        if (loggedInUserId != customerId)
        {
            return Forbid();
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(customer => customer.Id == customerId);

        if (customer is null)
        {
            return NotFound();
        }

        customer.FullName = request.FullName;
        customer.Email = request.Email;

        await _context.SaveChangesAsync();

        return Ok(new Customer
        {
            FullName = customer.FullName,
            Email = customer.Email
        });
    }
}

public record UpdateCustomerRequest(
    string FullName,
    string Email);