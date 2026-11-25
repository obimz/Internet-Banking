using System.Security.Claims;
using FakeItEasy;
using FluentAssertions;
using Internet_Banking.Controllers;
using Internet_Banking.Data;
using Internet_Banking.DTOs.Trasactions;
using Internet_Banking.Models;
using Internet_Banking.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InternetBankingTests;

public class TransactionsControllerTests
{
    private readonly Internet_BankingDbContext _db = InMemoryDbHelper.CreateContext();
    private readonly ITransactionService _transactionService = A.Fake<ITransactionService>();

    private static int _accountNumberSeed = 2000000000;

    // Builds the controller and attaches a logged-in user.
    private TransactionsController CreateSut(Guid loggedInCustomerId)
    {
        var controller = new TransactionsController(_db, _transactionService);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext//fake request
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, loggedInCustomerId.ToString()) },
                    "TestAuth"))
            }
        };

        return controller;
    }

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
            AccountNumber = (_accountNumberSeed++).ToString(),
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
    public async Task Deposit_ValidRequest_CreditsAccountAndRecordsTransaction()
    {
        //Arrange
        var account = SeedAccount(balance: 1000m);
        var sut = CreateSut(account.CustomerId);

        //Act
        var result = await sut.Deposit(new DepositRequest { AccountId = account.Id, Amount = 250m });

        //Assert
        result.Should().BeOfType<OkObjectResult>();
        account.Balance.Should().Be(1250m);

        var transaction = _db.Transactions.Single();
        transaction.TransactionType.Should().Be(TransactionType.Deposit);
        transaction.Amount.Should().Be(250m);
        transaction.SourceAccountId.Should().BeNull();           // money comes from outside the bank
        transaction.DestinationAccountId.Should().Be(account.Id);
    }

    [Fact]
    public async Task Deposit_AccountOwnedBySomeoneElse_Returns404_AndChangesNothing()
    {
        //Arrange
        var victim = SeedAccount(balance: 1000m);
        var sut = CreateSut(Guid.NewGuid()); // logged-in user does not own the account

        //Act
        var result = await sut.Deposit(new DepositRequest { AccountId = victim.Id, Amount = 250m });

        //Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        victim.Balance.Should().Be(1000m);
        _db.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Withdraw_ValidRequest_DebitsAccountAndRecordsTransaction()
    {
        //Arrange
        var account = SeedAccount(balance: 1000m);
        var sut = CreateSut(account.CustomerId);

        //Act
        var result = await sut.Withdraw(new WithdrawRequest { AccountId = account.Id, Amount = 400m });

        //Assert
        result.Should().BeOfType<OkObjectResult>();
        account.Balance.Should().Be(600m);

        var transaction = _db.Transactions.Single();
        transaction.TransactionType.Should().Be(TransactionType.Withdrawal);
        transaction.Amount.Should().Be(400m);
        transaction.SourceAccountId.Should().Be(account.Id);
        transaction.DestinationAccountId.Should().BeNull();      // money leaves the bank
    }

    [Fact]
    public async Task Withdraw_InsufficientFunds_ReturnsBadRequest_AndChangesNothing()
    {
        //Arrange
        var account = SeedAccount(balance: 100m);
        var sut = CreateSut(account.CustomerId);

        //Act: one kobo more than the balance
        var result = await sut.Withdraw(new WithdrawRequest { AccountId = account.Id, Amount = 100.01m });

        //Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        account.Balance.Should().Be(100m);
        _db.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Withdraw_AccountOwnedBySomeoneElse_Returns404_AndChangesNothing()
    {
        //Arrange
        var victim = SeedAccount(balance: 1000m);
        var sut = CreateSut(Guid.NewGuid()); // logged-in user does not own the account

        //Act
        var result = await sut.Withdraw(new WithdrawRequest { AccountId = victim.Id, Amount = 400m });

        //Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        victim.Balance.Should().Be(1000m);
        _db.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Deposit_InactiveAccount_ReturnsBadRequest_AndChangesNothing()
    {
        //Arrange
        var account = SeedAccount(balance: 1000m, status: AccountStatus.Frozen);
        var sut = CreateSut(account.CustomerId); // the owner, so ownership passes and the status check is reached

        //Act
        var result = await sut.Deposit(new DepositRequest { AccountId = account.Id, Amount = 250m });

        //Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        account.Balance.Should().Be(1000m);
        _db.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Withdraw_InactiveAccount_ReturnsBadRequest_AndChangesNothing()
    {
        //Arrange
        var account = SeedAccount(balance: 1000m, status: AccountStatus.Frozen);
        var sut = CreateSut(account.CustomerId);

        //Act
        var result = await sut.Withdraw(new WithdrawRequest { AccountId = account.Id, Amount = 400m });

        //Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        account.Balance.Should().Be(1000m);
        _db.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task TransferInternal_Success_CallsServiceWithLoggedInCustomerAndReturnsData()
    {
        //Arrange
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var destinationId = Guid.NewGuid();
        A.CallTo(() => _transactionService.ExecuteInternalTransferAsync(
                A<Guid>._, A<Guid>._, A<Guid>._, A<decimal>._, A<string?>._))
            .Returns(new ServiceResult(true, 200, Data: "transfer-data"));
        var sut = CreateSut(customerId);

        //Act
        var result = await sut.TransferInternal(new TransferInternalRequest
        {
            SourceAccountId = sourceId,
            DestinationAccountId = destinationId,
            Amount = 75m,
            Narration = "rent"
        });

        //Assert: the controller passed the logged-in customer from the token plus the request values
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be("transfer-data");
        A.CallTo(() => _transactionService.ExecuteInternalTransferAsync(
                customerId, sourceId, destinationId, 75m, "rent"))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task TransferInternal_ServiceFailure_ReturnsServiceStatusCode()
    {
        //Arrange
        A.CallTo(() => _transactionService.ExecuteInternalTransferAsync(
                A<Guid>._, A<Guid>._, A<Guid>._, A<decimal>._, A<string?>._))
            .Returns(new ServiceResult(false, 400, "Insufficient funds in source account."));
        var sut = CreateSut(Guid.NewGuid());

        //Act
        var result = await sut.TransferInternal(new TransferInternalRequest
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 75m
        });

        //Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
        objectResult.Value.Should().Be("Insufficient funds in source account.");
    }
}
