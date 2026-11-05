namespace Internet_Banking.Models;

public class BankAccount
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public AccountType AccountType { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.Active;

    public string Currency { get; set; } = "NGN";
    public decimal Balance { get; set; }
    public decimal DailyTransferLimit { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}