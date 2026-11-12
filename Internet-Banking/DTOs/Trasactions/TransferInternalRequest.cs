using System.ComponentModel.DataAnnotations;

namespace Internet_Banking.DTOs.Trasactions;

public class TransferInternalRequest
{
    [Required]
    public Guid SourceAccountId { get; set; }

    [Required]
    public Guid DestinationAccountId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Transfer amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [StringLength(200)]
    public string? Narration { get; set; }
}