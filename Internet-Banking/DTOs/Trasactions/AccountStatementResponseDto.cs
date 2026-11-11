using Internet_Banking.DTOs.Trasactions;

namespace Internet_Banking.DTOs;

public class AccountStatementResponseDto
{
    public Guid AccountId { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal NetChange { get; set; }
    public List<TransactionStatementItemDto> Transactions { get; set; } = new();
}