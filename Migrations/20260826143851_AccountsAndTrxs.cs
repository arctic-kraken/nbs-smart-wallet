using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace nbs_smart_wallet.Migrations
{
    /// <inheritdoc />
    public partial class AccountsAndTrxs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RevAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AspNetUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    AccountType = table.Column<string>(type: "text", nullable: false),
                    AccountSubType = table.Column<string>(type: "text", nullable: false),
                    Nickname = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RevBankAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RevAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemeName = table.Column<string>(type: "text", nullable: false),
                    Identification = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SecondaryIdentification = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevBankAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RevTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RevAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    BookingDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValueDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BalanceCurrency = table.Column<string>(type: "text", nullable: false),
                    BalanceAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrencyExchangeJson = table.Column<string>(type: "text", nullable: false),
                    CreditDebitIndicator = table.Column<string>(type: "text", nullable: false),
                    RevCreditorAccountJson = table.Column<string>(type: "text", nullable: false),
                    RevDebtorAccountJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TransactionInformation = table.Column<string>(type: "text", nullable: false),
                    SupplementaryData = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevTransactions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RevAccounts");

            migrationBuilder.DropTable(
                name: "RevBankAccounts");

            migrationBuilder.DropTable(
                name: "RevTransactions");
        }
    }
}
