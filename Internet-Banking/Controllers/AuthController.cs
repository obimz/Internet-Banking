using Internet_Banking.Data;
using Internet_Banking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace Internet_Banking.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : Controller
{
    private readonly Internet_BankingDbContext _context; //Dependency injection of the database context to access the database
    private readonly IPasswordHasher<Customer> _passwordHasher;//Dependency injection of the password hasher to hash and verify passwords

    private readonly IConfiguration _configuration;

    public AuthController(Internet_BankingDbContext context, IPasswordHasher<Customer> passwordHasher, IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
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

        var token = CreateToken(customer);

        return Ok(new
        {
            token
        });
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




private string CreateToken(Customer customer)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT signing key is not configured.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer is not configured.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience is not configured.");

        var expiresInMinutes =
            _configuration.GetValue<int>("Jwt:ExpiresInMinutes", 60);

        var claims = new[]
        {
        new Claim(
            ClaimTypes.NameIdentifier,
            customer.Id.ToString()),

        new Claim(
            ClaimTypes.Email,
            customer.Email),

        new Claim(
            ClaimTypes.Name,
            customer.FullName)
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key));

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token); //converts the JWT token to a string representation that can be sent to the client
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

