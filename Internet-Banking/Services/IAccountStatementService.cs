using Internet_Banking.DTOs;
using Internet_Banking.DTOs.Trasactions;

namespace Internet_Banking.Service;

public interface IAccountStatementService
{
    Task<AccountStatementResponseDto> GetAccountStatementAsync(Guid accountId, AccountStatementFilterDto filter);
}

