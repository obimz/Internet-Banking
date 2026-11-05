using Internet_Banking.Models;
using Microsoft.EntityFrameworkCore;

namespace Internet_Banking.Data
{ 
    public class Internet_BankingDbContext : DbContext
    {
        public Internet_BankingDbContext(DbContextOptions<Internet_BankingDbContext> options) : base(options) { }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<BankAccount> BankAccounts => Set<BankAccount>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BankAccount>()
                .Property(account => account.Balance)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BankAccount>()
                .Property(account => account.DailyTransferLimit)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BankAccount>()
                .HasIndex(account => account.AccountNumber)
                .IsUnique();

            modelBuilder.Entity<Customer>()
                .HasMany(customer => customer.BankAccounts) //customer can have multiple bank accounts
                .WithOne(account => account.Customer) //Each account belongs to only one customer
                .HasForeignKey(account => account.CustomerId);            
        }



    }
}