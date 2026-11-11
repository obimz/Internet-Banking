using System.ComponentModel.DataAnnotations;

namespace Internet_Banking.DTOs.Trasactions
{
    public class WithdrawRequest
    {
        [Required]
        public Guid AccountId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Withdrawal amount must be greater than zero.")]
        public decimal Amount { get; set; }
    }
}
