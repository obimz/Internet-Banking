using Internet_Banking.Data;
using Internet_Banking.Models;
using Internet_Banking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace Internet_Banking.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly Internet_BankingDbContext _context; //Dependency injection of the database context to access the database
    private readonly IPasswordHasher<Customer> _passwordHasher;//Dependency injection of the password hasher to hash and verify passwords

    private readonly ITokenService _tokenService;


    private readonly IAccountService _accountService;

    public AuthController(Internet_BankingDbContext context, IPasswordHasher<Customer> passwordHasher, IAccountService accountService, ITokenService tokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _accountService = accountService;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.PhoneNumber) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Full name, email, phone number, and password are required.");
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

        var account = await _accountService.CreateAccountAsync(
            customer.Id,
            request.AccountType,
            0m // Initial deposit is set to 0
        );



        await _context.SaveChangesAsync();

        return Created(
            $"api/customers/specific-customer-details/{customer.Id}",
            new
            {
                customer.FullName,
                customer.Email,
                AccountNumber = account.AccountNumber,
                AccountType = account.AccountType,
                Balance = account.Balance,
            });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var customer = await _context.Customers
            .FirstOrDefaultAsync(customer => customer.Email == email);

        if (customer is null)
        {
            return Unauthorized("Invalid email or password.");
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword( //verifies hashed password
            customer,
            customer.PasswordHash,
            request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid password.");
        }

        var token = _tokenService.CreateToken(customer);

        return Ok(new 
        { Token = token });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest("Current password and new password are required.");
        }

        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(customerIdValue, out var customerId))
        {
            return Unauthorized();
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer is null)
        {
            return NotFound();
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(
            customer,
            customer.PasswordHash,
            request.CurrentPassword);

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            return BadRequest("Current password is incorrect.");
        }

        customer.PasswordHash = _passwordHasher.HashPassword(customer, request.NewPassword);

        await _context.SaveChangesAsync();

        return Ok("Password changed successfully.");
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(customerIdValue, out var customerId))
        {
            return Unauthorized();
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer is null)
        {
            return NotFound();
        }

        //JWT access tokens are stateless, meaning they don't require server-side storage.
        //Therefore, logging out is typically handled on the client side by removing the token from local storage or cookies.
        return Ok(new
        {
            message = "Logged out successfully."
        });
    }

}

public record RegisterRequest(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    AccountType AccountType);

public record LoginRequest(
    string Email,
    string Password);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

