using FluentAssertions;
using Internet_Banking.Data;
using Internet_Banking.Models;
using Internet_Banking.Services;

namespace InternetBankingTests;

public class TransactionServiceTests
{
    private readonly Internet_BankingDbContext _db = InMemoryDbHelper.CreateContext();

    private TransactionService CreateSut() => new(_db);

    private static int _accountNumberSeed = 1000000000;

    // Creates a customer who owns one bank account, and saves both to the in-memory database.
    private BankAccount SeedAccount(decimal balance = 1000m, AccountStatus status = AccountStatus.Active)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = "Test Customer",
            Email = $"{Guid.NewGuid():N}@test.com"
        };

        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            AccountNumber = (_accountNumberSeed++).ToString(), // unique per account
            Balance = balance,
            Status = status,
            AccountType = AccountType.Savings
        };

        _db.Customers.Add(customer);
        _db.BankAccounts.Add(account);
        _db.SaveChanges();

        return account;
    }

    [Fact]
    public async Task Transfer_ValidRequest_MovesMoneyAndRecordsTransaction()
    {
        //Arrange
        var source = SeedAccount(balance: 1000m);
        var destination = SeedAccount(balance: 50m);
        var sut = CreateSut();

        //Act
        var result = await sut.ExecuteInternalTransferAsync(
            source.CustomerId, source.Id, destination.Id, 300m, "stipend");

        //Assert
        result.IsSuccess.Should().BeTrue();
        source.Balance.Should().Be(700m);
        destination.Balance.Should().Be(350m);

        var transaction = _db.Transactions.Single();
        transaction.Amount.Should().Be(300m);
        transaction.SourceAccountId.Should().Be(source.Id);
        transaction.DestinationAccountId.Should().Be(destination.Id);
        transaction.TransactionStatus.Should().Be(TransactionStatus.Successful);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task Transfer_AmountNotPositive_Returns400_AndChangesNothing(decimal amount)
    {
        //Arrange
        var source = SeedAccount(balance: 500m);
        var destination = SeedAccount(balance: 500m);
        var sut = CreateSut();

        //Act
        var result = await sut.ExecuteInternalTransferAsync(
            source.CustomerId, source.Id, destination.Id, amount, null);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        source.Balance.Should().Be(500m);
        destination.Balance.Should().Be(500m);
        _db.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Transfer_SameSourceAndDestination_Returns400()
    {
        //Arrange
        var account = SeedAccount();
        var sut = CreateSut();

        //Act
        var result = await sut.ExecuteInternalTransferAsync(
            account.CustomerId, account.Id, account.Id, 10m, null);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        _db.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Transfer_InsufficientFunds_Returns400_AndChangesNothing()
    {
        //Arrange
        var source = SeedAccount(balance: 100m);
        var destination = SeedAccount(balance: 0m);
        var sut = CreateSut();

        //Act: one kobo more than the balance
        var result = await sut.ExecuteInternalTransferAsync(
            source.CustomerId, source.Id, destination.Id, 100.01m, null);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        source.Balance.Should().Be(100m);
        destination.Balance.Should().Be(0m);
        _db.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Transfer_SourceAccountOwnedBySomeoneElse_Returns404_AndChangesNothing() //proves customer can't take out money from another customer's account
    {
        //Arrange
        var victim = SeedAccount(balance: 1000m);
        var destination = SeedAccount(balance: 0m);
        var attackerCustomerId = Guid.NewGuid(); // does not own the victim's account
        var sut = CreateSut();

        //Act
        var result = await sut.ExecuteInternalTransferAsync(
            attackerCustomerId, victim.Id, destination.Id, 500m, null);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        victim.Balance.Should().Be(1000m);
        destination.Balance.Should().Be(0m);
        _db.Transactions.Should().BeEmpty();
    }

    [Theory]
    [InlineData(AccountStatus.Frozen, AccountStatus.Active)] // source not active
    [InlineData(AccountStatus.Active, AccountStatus.Closed)] // destination not active
    public async Task Transfer_AccountNotActive_Returns400_AndChangesNothing(
        AccountStatus sourceStatus, AccountStatus destinationStatus)
    {
        //Arrange
        var source = SeedAccount(balance: 500m, status: sourceStatus);
        var destination = SeedAccount(balance: 500m, status: destinationStatus);
        var sut = CreateSut();

        //Act
        var result = await sut.ExecuteInternalTransferAsync(
            source.CustomerId, source.Id, destination.Id, 10m, null);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        source.Balance.Should().Be(500m);
        destination.Balance.Should().Be(500m);
        _db.Transactions.Should().BeEmpty();
    }
}
