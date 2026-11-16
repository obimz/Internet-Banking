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
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllCustomers()
    {
        var customers = await _context.Customers
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.FullName,
                c.Email,
                c.PhoneNumber,
                c.Role,
                c.CreatedAt
            })
            .ToListAsync();

        return Ok(customers);
    }

    [HttpGet("specific-customer-details/{customerId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetCustomerAccount(Guid customerId)
    {
        var loggedInUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(loggedInUserIdValue, out var loggedInUserId))
        {
            return Unauthorized();
        }

        // [FIX 3]: Allow access if the user owns this profile OR is an Admin
        if (loggedInUserId != customerId && !User.IsInRole("Admin"))
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
    [Authorize]
    public async Task<IActionResult> UpdateCustomerAccount(
        Guid customerId, UpdateCustomerRequest request)
    {
        var loggedInUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(loggedInUserIdValue, out var loggedInUserId))
        {
            return Unauthorized();
        }

        if (loggedInUserId != customerId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer is null)
        {
            return NotFound();
        }

        var emailTaken = await _context.Customers
            .AnyAsync(c => c.Email == request.Email && c.Id != customerId);

        if (emailTaken)
        {
            return BadRequest("Email is already in use by another account.");
        }

        customer.FullName = request.FullName;
        customer.Email = request.Email;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            customer.FullName,
            customer.Email
        });
    }
}

public record UpdateCustomerRequest(
    string FullName,
    string Email);