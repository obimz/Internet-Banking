using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Internet_Banking.Migrations
{
    /// <inheritdoc />
    public partial class RowVersionBankAccountTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "BankAccounts",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "BankAccounts");
        }
    }
}
