using Internet_Banking.Models;
using Microsoft.EntityFrameworkCore;

namespace Internet_Banking.Data
{
    public class Internet_BankingDbContext : DbContext
    {
        public Internet_BankingDbContext(DbContextOptions<Internet_BankingDbContext> options) : base(options) { }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<BankAccount> BankAccounts => Set<BankAccount>();

        public DbSet<Transaction> Transactions => Set<Transaction>();

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

            modelBuilder.Entity<BankAccount>()//stores the enum values as strings in the database instead of integers
                .Property(account => account.AccountType)
                .HasConversion<string>();

            modelBuilder.Entity<BankAccount>()
                .Property(account => account.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Customer>()
                .HasMany(customer => customer.BankAccounts) //customer can have multiple bank accounts
                .WithOne(account => account.Customer) //Each account belongs to only one customer
                .HasForeignKey(account => account.CustomerId);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.TransactionReference)
                .IsUnique();

            modelBuilder.Entity<Transaction>()
                .Property(t => t.TransactionType)
                .HasConversion<string>();

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.SourceAccount)
                .WithMany()
                .HasForeignKey(t => t.SourceAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.DestinationAccount)
                .WithMany()
                .HasForeignKey(t => t.DestinationAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        }



    }
}