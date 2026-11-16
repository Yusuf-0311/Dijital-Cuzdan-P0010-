using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureWallet.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountLockout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Transactions_Payment_Biller",
                table: "Transactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transactions_Type_Wallets",
                table: "Transactions");

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transactions_Payment_Biller",
                table: "Transactions",
                sql: "(\"Type\" = 'Payment'\r\n    AND \"BillerId\" IS NOT NULL AND \"SubscriberNo\" IS NOT NULL)\r\nOR (\"Type\" <> 'Payment' AND \"BillerId\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transactions_Type_Wallets",
                table: "Transactions",
                sql: "(\"Type\" = 'Deposit'\r\n    AND \"SenderWalletId\" IS NULL AND \"ReceiverWalletId\" IS NOT NULL)\r\nOR (\"Type\" IN ('Withdraw', 'Payment')\r\n    AND \"SenderWalletId\" IS NOT NULL AND \"ReceiverWalletId\" IS NULL)\r\nOR (\"Type\" = 'Transfer'\r\n    AND \"SenderWalletId\" IS NOT NULL AND \"ReceiverWalletId\" IS NOT NULL\r\n    AND \"SenderWalletId\" <> \"ReceiverWalletId\")");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Transactions_Payment_Biller",
                table: "Transactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transactions_Type_Wallets",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "FailedLoginCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "Users");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transactions_Payment_Biller",
                table: "Transactions",
                sql: "(\"Type\" = 'Payment'\n    AND \"BillerId\" IS NOT NULL AND \"SubscriberNo\" IS NOT NULL)\nOR (\"Type\" <> 'Payment' AND \"BillerId\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transactions_Type_Wallets",
                table: "Transactions",
                sql: "(\"Type\" = 'Deposit'\n    AND \"SenderWalletId\" IS NULL AND \"ReceiverWalletId\" IS NOT NULL)\nOR (\"Type\" IN ('Withdraw', 'Payment')\n    AND \"SenderWalletId\" IS NOT NULL AND \"ReceiverWalletId\" IS NULL)\nOR (\"Type\" = 'Transfer'\n    AND \"SenderWalletId\" IS NOT NULL AND \"ReceiverWalletId\" IS NOT NULL\n    AND \"SenderWalletId\" <> \"ReceiverWalletId\")");
        }
    }
}
