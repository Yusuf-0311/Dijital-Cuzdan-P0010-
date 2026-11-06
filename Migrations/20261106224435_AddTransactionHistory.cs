using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureWallet.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Transactions",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "SenderWalletId",
                table: "Transactions",
                newName: "WalletId");

            migrationBuilder.RenameColumn(
                name: "ReceiverWalletId",
                table: "Transactions",
                newName: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_WalletId",
                table: "Transactions",
                column: "WalletId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Wallets_WalletId",
                table: "Transactions",
                column: "WalletId",
                principalTable: "Wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Wallets_WalletId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_WalletId",
                table: "Transactions");

            migrationBuilder.RenameColumn(
                name: "WalletId",
                table: "Transactions",
                newName: "SenderWalletId");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Transactions",
                newName: "ReceiverWalletId");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Transactions",
                newName: "Status");
        }
    }
}
