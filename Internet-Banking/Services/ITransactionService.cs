using Internet_Banking.DTOs.Trasactions;

namespace Internet_Banking.Services
{
    public interface ITransactionService
    {
        Task<ServiceResult> ExecuteInternalTransferAsync(
        Guid customerId,
        Guid sourceAccountId,
        Guid destinationAccountId,
        decimal amount,
        string? narration);
    }
}
