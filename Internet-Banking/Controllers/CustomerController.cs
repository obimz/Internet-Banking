using System.Security.Claims;
using Internet_Banking.Data;
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

    [HttpGet("GetCustomerAccount")]
    [Authorize]
    public async Task<IActionResult> GetCustomerAccount()
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(customerIdValue, out var customerId))
        {
            return Unauthorized();
        }

        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(customer => customer.Id == customerId);

        if (customer is null)
        {
            return NotFound();
        }

        return Ok(customer);
    }

    [HttpPut("UpdateCustomerAccount")]
    [Authorize]
    public async Task<IActionResult> UpdateCustomerAccount(
        UpdateCustomerRequest request)
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(customerIdValue, out var customerId))
        {
            return Unauthorized();
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

        return Ok(customer);
    }
}

public record UpdateCustomerRequest(
    string FullName,
    string Email);