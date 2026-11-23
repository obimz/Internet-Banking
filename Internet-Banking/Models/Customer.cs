
namespace Internet_Banking.Models
{
    public class Customer
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "Customer"; // Default role is set to "Customer"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<BankAccount> BankAccounts { get; set; } = new List<BankAccount>(); //Navigation property representing the collection of bank accounts associated with the customer

        public ICollection<Beneficiary> Beneficiaries { get; set; } = new List<Beneficiary>();
    }
}