using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nbs_smart_wallet.Migrations
{
    /// <inheritdoc />
    public partial class ChangeStringToEnumsPart2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "AccountType",
                table: "RevAccounts",
                type: "nvarchar(24)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "AccountSubType",
                table: "RevAccounts",
                type: "nvarchar(24)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "AccountType",
                table: "RevAccounts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "nvarchar(24)");

            migrationBuilder.AlterColumn<int>(
                name: "AccountSubType",
                table: "RevAccounts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "nvarchar(24)");
        }
    }
}
