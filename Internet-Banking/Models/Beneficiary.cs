namespace Internet_Banking.Models;

public class Beneficiary
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}