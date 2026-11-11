namespace Internet_Banking.Models;

public class Transaction
{
    public Guid Id { get; set; }

    public string TransactionReference { get; set; } = string.Empty;

    public TransactionType TransactionType { get; set; }

    public Guid? SourceAccountId { get; set; }
    public BankAccount? SourceAccount { get; set; }

    public Guid? DestinationAccountId { get; set; }
    public BankAccount? DestinationAccount { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = "Successful";

    public string? Narration { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}