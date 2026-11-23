using FluentAssertions;
using FakeItEasy;
using Internet_Banking.Data;
using Microsoft.AspNetCore.Identity;
using Internet_Banking.Models;
using Internet_Banking.Services;
using Internet_Banking.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace InternetBankingTests;

public class AuthControllerTests
{
    private readonly Internet_BankingDbContext _db = InMemoryDbHelper.CreateContext();
    private readonly IPasswordHasher<Customer> _hasher = A.Fake<IPasswordHasher<Customer>>();
    private readonly ITokenService _token = A.Fake<ITokenService>();
    private readonly IAccountService _account = A.Fake<IAccountService>();

    private AuthController CreateSut() =>
        new(_db, _hasher, A.Fake<IConfiguration>(), _account, _token);

    // Puts a customer into the in-memory database so Login/Register can find it.
    private Customer SeedCustomer(string email = "ada@test.com")
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = "Ada",
            Email = email,
            PasswordHash = "hash"
        };
        _db.Customers.Add(customer);
        _db.SaveChanges();
        return customer;
    }


    [Fact]
    public async Task Login_InvalidEmail_RejectWithNoToken()
    {
        //Arrange
        var sut = CreateSut();

        //Act
        var result = await sut.Login(new LoginRequest
            ("nobody@test.com", "doesntmatter"));
        
        //Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        A.CallTo(() => _token.CreateToken(A<Customer>._)).MustNotHaveHappened();
    }

   

    [Fact]
    public async Task Login_WrongPassword_RejectWithNoToken()
    {
        //Arrange
        SeedCustomer();
        A.CallTo(() => _hasher.VerifyHashedPassword(A<Customer>._, A<string>._, A<string>._))
            .Returns(PasswordVerificationResult.Failed);
        var sut = CreateSut();

        //Act
        var result = await sut.Login(new LoginRequest("ada@test.com", "wrong"));

        //Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        A.CallTo(() => _token.CreateToken(A<Customer>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        //Arrange
        var customer = SeedCustomer("ada@test.com");
        A.CallTo(() => _hasher.VerifyHashedPassword(A<Customer>._, A<string>._, A<string>._))
            .Returns(PasswordVerificationResult.Success);
        A.CallTo(() => _token.CreateToken(customer)).Returns("jwt-token");
        var sut = CreateSut();

        //Act: email is padded and uppercase, Login should trim and lowercase it
        var result = await sut.Login(new LoginRequest("  ADA@Test.com ", "pw"));

        //Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value!.ToString().Should().Contain("jwt-token");
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict_AndCreatesNothing()
    {
        //Arrange
        SeedCustomer("ada@test.com");
        var sut = CreateSut();

        //Act: same email, different casing and spacing
        var result = await sut.Register(
            new RegisterRequest("Ada Two", " ADA@test.com ", "0800", "pw", AccountType.Savings));

        //Assert
        result.Should().BeOfType<ConflictObjectResult>();
        _db.Customers.Count().Should().Be(1);
        A.CallTo(() => _account.CreateAccountAsync(A<Guid>._, A<AccountType>._, A<decimal>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Register_MissingFields_ReturnsBadRequest()
    {
        //Arrange
        var sut = CreateSut();

        //Act: empty full name
        var result = await sut.Register(
            new RegisterRequest("", "a@b.com", "0800", "pw", AccountType.Savings));

        //Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        _db.Customers.Should().BeEmpty();
    }

    [Fact]
    public async Task Register_ValidRequest_SavesCustomerWithHashedPassword()
    {
        //Arrange
        A.CallTo(() => _hasher.HashPassword(A<Customer>._, "pw")).Returns("hashed-pw");
        A.CallTo(() => _account.CreateAccountAsync(A<Guid>._, AccountType.Savings, 0m))
            .Returns(new BankAccount { AccountNumber = "1234567890", AccountType = AccountType.Savings });
        var sut = CreateSut();

        //Act
        var result = await sut.Register(
            new RegisterRequest("Ada", "ADA@test.com", "0800", "pw", AccountType.Savings));

        //Assert
        result.Should().BeOfType<CreatedResult>();
        var saved = _db.Customers.Single();
        saved.Email.Should().Be("ada@test.com");     // normalised to lowercase
        saved.PasswordHash.Should().Be("hashed-pw"); // never the plain password
    }
}
