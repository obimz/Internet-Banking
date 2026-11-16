
using Internet_Banking.Models;

namespace Internet_Banking.Services;

public interface IAccountService
{
    Task<BankAccount> CreateAccountAsync(Guid customerId, AccountType accountType, decimal initialDeposit);
}