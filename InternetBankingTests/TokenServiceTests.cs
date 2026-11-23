using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Internet_Banking.Models;
using Internet_Banking.Services;
using Microsoft.Extensions.Configuration;

namespace InternetBankingTests;

public class TokenServiceTests
{
    // Builds a real IConfiguration from a dictionary.
    private static IConfiguration BuildConfig(Dictionary<string, string?> settings) =>
        new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

    private static Dictionary<string, string?> ValidSettings() => new()
    {
        ["Jwt:Key"] = "a-test-signing-key-that-is-at-least-32-chars-long!", // 32+ chars required by HMAC-SHA256
        ["Jwt:Issuer"] = "test-issuer",
        ["Jwt:Audience"] = "test-audience"
    };

    [Fact]
    public void CreateToken_ValidCustomer_ContainsExpectedClaims()
    {
        // Arrange
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = "Ada Lovelace",
            Email = "ada@test.com",
            Role = "Admin"
        };
        var sut = new TokenService(BuildConfig(ValidSettings()));

        // Act
        var token = sut.CreateToken(customer);

        // Assert: decode the token string and inspect what's inside
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Issuer.Should().Be("test-issuer");
        jwt.Audiences.Should().Contain("test-audience");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == customer.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "ada@test.com");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Theory]
    [InlineData("Jwt:Key")]
    [InlineData("Jwt:Issuer")]
    [InlineData("Jwt:Audience")]
    public void CreateToken_MissingRequiredSetting_Throws(string missingKey)
    {
        // Arrange: valid config minus one required setting
        var settings = ValidSettings();
        settings.Remove(missingKey);
        var sut = new TokenService(BuildConfig(settings));
        var customer = new Customer { Id = Guid.NewGuid(), FullName = "Ada", Email = "ada@test.com" };

        // Act
        var act = () => sut.CreateToken(customer);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }
}
