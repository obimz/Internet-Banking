namespace Internet_Banking.Models;

public class IdempotencyRecord
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; } //keys are scoped per customer

    public string IdempotencyKey { get; set; } = string.Empty;

    public int? StatusCode { get; set; }
    public string? ResponseBody { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
