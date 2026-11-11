using System.ComponentModel.DataAnnotations;

namespace Internet_Banking.DTOs.Trasactions;

public class DomesticTransferRequest
{
    [Required]
    public Guid SourceAccountId { get; set; }

    [Required]
    [StringLength(10, MinimumLength = 3)]
    public string DestinationBankCode { get; set; } = string.Empty;

    [Required]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Destination account number must be 10 digits.")]
    public string DestinationAccountNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string DestinationAccountName { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Transfer amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [StringLength(200)]
    public string? Narration { get; set; }
}