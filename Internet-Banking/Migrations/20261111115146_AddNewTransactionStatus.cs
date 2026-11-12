using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Internet_Banking.Migrations
{
    /// <inheritdoc />
    public partial class AddNewTransactionStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Transactions",
                newName: "TransactionStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TransactionStatus",
                table: "Transactions",
                newName: "Status");
        }
    }
}
