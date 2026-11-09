using System.ComponentModel.DataAnnotations;

namespace Internet_Banking.DTOs.Trasactions;

public class DepositRequest
{
    [Required]
    public Guid AccountId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Deposit amount must be greater than zero.")]
    public decimal Amount { get; set; }
}