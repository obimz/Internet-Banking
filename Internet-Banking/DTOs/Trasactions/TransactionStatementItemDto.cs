using Internet_Banking.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Internet_Banking.DTOs.Trasactions
{
    public class TransactionStatementItemDto
    {
        public string TransactionReference { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        // Dynamically evaluated in LINQ as Credit or Debit relative to the target account
        public string FlowType { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
        public string? Narration { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
