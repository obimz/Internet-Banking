using System.Security.Claims;
using Internet_Banking.Data;
using Internet_Banking.Models;
using Internet_Banking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Internet_Banking.Controllers;

[ApiController]
[Route("api/beneficiaries")]
[Authorize]
public class BeneficiaryController : ControllerBase
{
    private readonly Internet_BankingDbContext _context;
    private readonly ITransactionService _transactionService;

    public BeneficiaryController(Internet_BankingDbContext context, ITransactionService transactionService)
    {
        _context = context;
        _transactionService = transactionService;
    }

    [HttpPost("add-beneficiary")]
    public async Task<IActionResult> AddBeneficiary(AddBeneficiaryRequest request)
    {
        var loggedInUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(loggedInUserIdValue, out var loggedInUserId))
        {
            return Unauthorized();
        }

        var targetAccount = await _context.BankAccounts
            .AsNoTracking()
            .Where(a => a.AccountNumber == request.AccountNumber)
            .Join(_context.Customers,
                account => account.CustomerId,
                customer => customer.Id,
                (account, customer) => new
                {
                    account.AccountNumber,
                    account.CustomerId,
                    CustomerFullName = customer.FullName
                })
            .FirstOrDefaultAsync();

        if (targetAccount is null)
        {
            return NotFound("Bank account with that account number was not found.");
        }

        if (targetAccount.CustomerId == loggedInUserId)
        {
            return BadRequest("You cannot add your own bank account as a beneficiary.");
        }

        var alreadyExists = await _context.Beneficiaries
            .AnyAsync(b => b.CustomerId == loggedInUserId && b.AccountNumber == request.AccountNumber);

        if (alreadyExists)
        {
            return BadRequest("This beneficiary is already saved to your account.");
        }

        var beneficiary = new Beneficiary
        {
            Id = Guid.NewGuid(),
            CustomerId = loggedInUserId,
            AccountNumber = targetAccount.AccountNumber,
            AccountName = targetAccount.CustomerFullName,
            Nickname = request.Nickname,
            CreatedAt = DateTime.UtcNow
        };

        _context.Beneficiaries.Add(beneficiary);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            beneficiary.Id,
            beneficiary.AccountNumber,
            beneficiary.AccountName,
            beneficiary.Nickname,
            beneficiary.CreatedAt
        });
    }

    [HttpGet("view-beneficiaries")]
    public async Task<IActionResult> ViewSavedBeneficiaries()
    {
        var loggedInUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(loggedInUserIdValue, out var loggedInUserId))
        {
            return Unauthorized();
        }

        var beneficiaries = await _context.Beneficiaries
            .AsNoTracking()
            .Where(b => b.CustomerId == loggedInUserId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                b.Id,
                b.AccountNumber,
                b.AccountName,
                b.Nickname,
                b.CreatedAt
            })
            .ToListAsync();

        return Ok(beneficiaries);
    }

    [HttpPut("update-beneficiary/{beneficiaryId:guid}")]
    public async Task<IActionResult> UpdateBeneficiaryDetails(
        Guid beneficiaryId, UpdateBeneficiaryRequest request)
    {
        var loggedInUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(loggedInUserIdValue, out var loggedInUserId))
        {
            return Unauthorized();
        }

        var beneficiary = await _context.Beneficiaries
            .FirstOrDefaultAsync(b => b.Id == beneficiaryId);

        if (beneficiary is null)
        {
            return NotFound("Beneficiary not found.");
        }

        if (beneficiary.CustomerId != loggedInUserId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        if (!string.IsNullOrWhiteSpace(request.AccountNumber) &&
            request.AccountNumber != beneficiary.AccountNumber)
        {
            var newTargetAccount = await _context.BankAccounts
                .AsNoTracking()
                .Where(a => a.AccountNumber == request.AccountNumber)
                .Join(_context.Customers,
                    account => account.CustomerId,
                    customer => customer.Id,
                    (account, customer) => new
                    {
                        account.AccountNumber,
                        account.CustomerId,
                        CustomerFullName = customer.FullName
                    })
                .FirstOrDefaultAsync();

            if (newTargetAccount is null)
            {
                return NotFound("The new bank account number was not found.");
            }

            if (newTargetAccount.CustomerId == beneficiary.CustomerId)
            {
                return BadRequest("You cannot set your own bank account as a beneficiary.");
            }

            var duplicate = await _context.Beneficiaries
                .AnyAsync(b => b.CustomerId == beneficiary.CustomerId
                            && b.AccountNumber == request.AccountNumber
                            && b.Id != beneficiaryId);

            if (duplicate)
            {
                return BadRequest("Another saved beneficiary already uses this account number.");
            }

            beneficiary.AccountNumber = newTargetAccount.AccountNumber;
            beneficiary.AccountName = newTargetAccount.CustomerFullName;
        }

        beneficiary.Nickname = request.Nickname;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            beneficiary.Id,
            beneficiary.AccountNumber,
            beneficiary.AccountName,
            beneficiary.Nickname
        });
    }

    [HttpDelete("delete-beneficiary/{beneficiaryId:guid}")]
    public async Task<IActionResult> DeleteBeneficiary(Guid beneficiaryId)
    {
        var loggedInUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(loggedInUserIdValue, out var loggedInUserId))
        {
            return Unauthorized();
        }

        var beneficiary = await _context.Beneficiaries
            .FirstOrDefaultAsync(b => b.Id == beneficiaryId);

        if (beneficiary is null)
        {
            return NotFound("Beneficiary not found.");
        }

        if (beneficiary.CustomerId != loggedInUserId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        _context.Beneficiaries.Remove(beneficiary);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Beneficiary deleted successfully." });
    }

    [HttpPost("transfer/{beneficiaryId:guid}")]
    public async Task<IActionResult> TransferToBeneficiary(
        Guid beneficiaryId, BeneficiaryTransferRequest request)
    {
        var loggedInUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(loggedInUserIdValue, out var loggedInUserId))
        {
            return Unauthorized();
        }

        
        var customer = await _context.Customers
            .AsNoTracking()
            .Include(c => c.Beneficiaries)
            .FirstOrDefaultAsync(c => c.Id == loggedInUserId);

        if (customer is null)
        {
            return Unauthorized();
        }

        var beneficiary = customer.Beneficiaries
            .FirstOrDefault(b => b.Id == beneficiaryId);

        if (beneficiary is null)
        {
            return NotFound("Beneficiary not found.");
        }

        var destinationAccountId = await _context.BankAccounts
            .AsNoTracking()
            .Where(a => a.AccountNumber == beneficiary.AccountNumber)
            .Select(a => a.Id)
            .FirstOrDefaultAsync();

        if (destinationAccountId == Guid.Empty)
        {
            return NotFound("Beneficiary's bank account could not be found.");
        }

        var result = await _transactionService.ExecuteInternalTransferAsync(
            loggedInUserId,
            request.SourceAccountId,
            destinationAccountId,
            request.Amount,
            request.Narration ?? $"Transfer to {beneficiary.Nickname ?? beneficiary.AccountName}");

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, result.ErrorMessage);
        }

        return Ok(result.Data);
    }
}

public record AddBeneficiaryRequest(
    string AccountNumber,
    string? Nickname);

public record UpdateBeneficiaryRequest(
    string AccountNumber,
    string? Nickname);

public record BeneficiaryTransferRequest(
    Guid SourceAccountId,
    decimal Amount,
    string? Narration);