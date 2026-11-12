using System.ComponentModel.DataAnnotations;

namespace Internet_Banking.DTOs.Trasactions;

public class AccountStatementFilterDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public string? TransactionType { get; set; }
}

