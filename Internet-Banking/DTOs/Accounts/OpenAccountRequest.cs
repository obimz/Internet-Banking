using System.ComponentModel.DataAnnotations;
using Internet_Banking.Models;

namespace Internet_Banking.DTOs.Accounts;

public class OpenAccountRequest
{
    [Required]
    [EnumDataType(typeof(AccountType))]
    public AccountType AccountType { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Initial deposit cannot be negative.")]
    public decimal InitialDeposit { get; set; } = 0m;
}