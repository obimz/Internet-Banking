using Internet_Banking.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Internet_Banking.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string CreateToken(Customer customer)
    {
        var key = _config["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT signing key is not configured.");

        var issuer = _config["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer is not configured.");

        var audience = _config["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience is not configured.");

        var expiresInMinutes =
            _config.GetValue<int>("Jwt:ExpiresInMinutes", 60);

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
            customer.FullName),

        new Claim(
            ClaimTypes.Role,
            string.IsNullOrWhiteSpace(customer.Role) ? "Customer" : customer.Role)
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


