using Internet_Banking.Data;
using Internet_Banking.DTOs.Trasactions;
using Internet_Banking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Internet_Banking.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly Internet_BankingDbContext _context;

    public TransactionsController(Internet_BankingDbContext context)
    {
        _context = context;
    }

    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit(DepositRequest request)
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(customerIdValue, out var customerId))
        {
            return Unauthorized();
        }
        var account = await _context.BankAccounts
            .FirstOrDefaultAsync(a => a.CustomerId == customerId && a.Id == request.AccountId);

        if (account is null)
        {
            return NotFound("Account not found.");
        }
        account.Balance += request.Amount;
        await _context.SaveChangesAsync();

        var transactionReference = GenerateTransactionReference();
        return Ok(new
        {
            transactionReference,
            account.Id,
            account.Balance            
        });
    }

    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw(WithdrawRequest request)
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(customerIdValue, out var customerId))
        {
            return Unauthorized();
        }
        var account = await _context.BankAccounts
            .FirstOrDefaultAsync(a => a.CustomerId == customerId && a.Id == request.AccountId);

        if (account is null)
        {
            return NotFound("Account not found.");
        }

        if (account.Balance < request.Amount)
        {
            return BadRequest("Insufficient funds.");
        }

        account.Balance -= request.Amount;

        await _context.SaveChangesAsync();

        var transactionReference = GenerateTransactionReference();
        return Ok(new
        {
            transactionReference,
            account.Id,
            account.Balance            
        });
    }

    [HttpPost("internal-transfer")]
    public async Task<IActionResult> TransferInternal(TransferInternalRequest request)
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(customerIdValue, out var customerId))
        {
            return Unauthorized();
        }

        if (request.Amount <= 0)
        {
            return BadRequest("Transfer amount must be greater than zero.");
        }

        if (request.SourceAccountId == request.DestinationAccountId)
        {
            return BadRequest("Source and destination accounts cannot be the same.");
        }

        var sourceAccount = await _context.BankAccounts
            .FirstOrDefaultAsync(a => a.CustomerId == customerId && a.Id == request.SourceAccountId);

        if (sourceAccount is null)
        {
            return NotFound("Source account not found.");
        }

        var destinationAccount = await _context.BankAccounts
            .FirstOrDefaultAsync(a => a.Id == request.DestinationAccountId);

        if (destinationAccount is null)
        {
            return NotFound("Destination account not found.");
        }

        if (sourceAccount.Status != AccountStatus.Active || destinationAccount.Status != AccountStatus.Active)
        {
            return BadRequest("Both accounts must be active.");
        }

        if (sourceAccount.Balance < request.Amount)
        {
            return BadRequest("Insufficient funds in source account.");
        }

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(); //Ensures that both operations are treated as a single atomic operation.

        sourceAccount.Balance -= request.Amount;
        destinationAccount.Balance += request.Amount;

        var transactionReference = GenerateTransactionReference();

        await _context.SaveChangesAsync();
        await dbTransaction.CommitAsync();

        return Ok(new
        {
            transactionReference,
            sourceAccountId = sourceAccount.Id,
            sourceAccountBalance = sourceAccount.Balance,
            destinationAccountId = destinationAccount.Id,
            destinationAccountBalance = destinationAccount.Balance
        });
    }

    [HttpPost("domestic-transfer")]
    public async Task<IActionResult> DomesticTransfer(DomesticTransferRequest request)
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(customerIdValue, out var customerId))
        {
            return Unauthorized();
        }

        if (request.Amount <= 0)
        {
            return BadRequest("Transfer amount must be greater than zero.");
        }

        var sourceAccount = await _context.BankAccounts
            .FirstOrDefaultAsync(a => a.Id == request.SourceAccountId && a.CustomerId == customerId);

        if (sourceAccount is null)
        {
            return NotFound("Source account not found.");
        }

        if (sourceAccount.Status != AccountStatus.Active)
        {
            return BadRequest("Source account is not active.");
        }

        if (sourceAccount.Balance < request.Amount)
        {
            return BadRequest("Insufficient funds.");
        }

        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        sourceAccount.Balance -= request.Amount;

        // Simulated transfer reference and success response
        var transactionReference = GenerateTransactionReference();
        var providerReference = $"SIM-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

        await _context.SaveChangesAsync();
        await dbTransaction.CommitAsync();

        return Ok(new
        {
            transactionReference,
            providerReference,
            status = "Successful",
            channel = "DomesticTransferSimulated",
            sourceAccountId = sourceAccount.Id,
            sourceAccountBalance = sourceAccount.Balance,
            destinationBankCode = request.DestinationBankCode,
            destinationAccountNumber = request.DestinationAccountNumber,
            destinationAccountName = request.DestinationAccountName,
            amount = request.Amount,
            narration = request.Narration
        });
    }

    [HttpGet("view-transactions/{accountId:guid}")]
    public async Task<IActionResult> ViewTransactions(Guid accountId)
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(customerIdValue, out var customerId))
        {
            return Unauthorized();
        }

        var ownsaccount = await _context.BankAccounts.AnyAsync(a => a.CustomerId == customerId && a.Id == accountId);//checks if the account belongs to the customer

        if (!ownsaccount)
            return NotFound("Account not found.");

        var transactions = await _context.Transactions
            .Where(t => t.SourceAccountId == accountId || t.DestinationAccountId == accountId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        
        return Ok(transactions);

    }

    [HttpGet("view-details/{transactionId:guid}")]//view details of a specific transaction by its ID
    public async Task<IActionResult> ViewTransactionDetails(Guid transactionId)
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(customerIdValue, out var customerId))
        {
            return Unauthorized();
        }

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction is null)
            return NotFound("Transaction not found.");

        var ownAccount = await _context.BankAccounts
            .AnyAsync(a => a.CustomerId == customerId && (a.Id == transaction.SourceAccountId || a.Id == transaction.DestinationAccountId));

        if (!ownAccount)
            return Forbid("You do not have permission to view this transaction. Check your account ownership.");

        return Ok(transaction);
    }


    private static string GenerateTransactionReference()
    {
        return $"TRX-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N[..8].ToUpperInvariant()}";
    }
}

