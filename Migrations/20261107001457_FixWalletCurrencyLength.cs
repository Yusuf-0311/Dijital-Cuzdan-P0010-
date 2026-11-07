using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureWallet.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixWalletCurrencyLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
    name: "Currency",
    table: "Wallets",
    type: "character varying(3)",
    maxLength: 3,
    nullable: false,
    oldClrType: typeof(string),
    oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
          migrationBuilder.AlterColumn<string>(
    name: "Currency",
    table: "Wallets",
    type: "text",
    nullable: false,
    oldClrType: typeof(string),
    oldType: "character varying(3)",
    oldMaxLength: 3);
        }
    }
}
