using Internet_Banking.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Internet_Banking.Models;

namespace Internet_Banking.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : Controller
{
    private readonly Internet_BankingDbContext _context;
    private readonly IPasswordHasher<Customer> _passwordHasher;

    public AuthController(Internet_BankingDbContext context, IPasswordHasher<Customer> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Full name, email, and password are required.");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var emailAlreadyExists = await _context.Customers.AnyAsync(customer => customer.Email == email);

        if (emailAlreadyExists)
        {
            return Conflict("A customer with this email already exists.");
        }
        
        var customer = new Customer
        {
            FullName = request.FullName,
            Email = email,
            PhoneNumber = request.PhoneNumber.Trim()
        };

        customer.PasswordHash = _passwordHasher.HashPassword(customer, request.Password); //Hashes the passworde
        _context.Customers.Add(customer);

        await _context.SaveChangesAsync();

        return Created(
            $"api/customer/{customer.Id}",
            new
            {
                customer.Id,
                customer.FullName,
                customer.Email
            });
    }

    [HttpPost("login")]
    [AllowAnonymous]//endpoint can be called without JWT token
    public IActionResult Login(LoginRequest request)
    {
        return Ok();
    }

    [HttpPost("change-password")]
    [Authorize]
    public IActionResult ChangePassword(ChangePasswordRequest request)
    {
        return Ok();
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        return Ok();
    }


}

public record RegisterRequest(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password);

public record LoginRequest(
    string Email,
    string Password);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

