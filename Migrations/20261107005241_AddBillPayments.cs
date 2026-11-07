using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SecureWallet.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBillPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BillerId",
                table: "Transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriberNo",
                table: "Transactions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Billers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubscriberNoLabel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Billers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    BillerId = table.Column<int>(type: "integer", nullable: false),
                    SubscriberNo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Nickname = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedPayments_Billers_BillerId",
                        column: x => x.BillerId,
                        principalTable: "Billers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SavedPayments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Billers",
                columns: new[] { "Id", "Category", "IsActive", "Name", "SubscriberNoLabel" },
                values: new object[,]
                {
                    { 1, "Electricity", true, "Kent Elektrik Dağıtım", "Tesisat No" },
                    { 2, "Water", true, "Şehir Su İdaresi", "Abone No" },
                    { 3, "NaturalGas", true, "Anadolu Doğalgaz", "Abone No" },
                    { 4, "Internet", true, "HızlıNet İnternet", "Hizmet No" },
                    { 5, "MobilePhone", true, "Mavi Mobil", "Telefon No" },
                    { 6, "Education", true, "Üniversite Harç Ödemeleri", "Öğrenci No" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BillerId",
                table: "Transactions",
                column: "BillerId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transactions_Payment_Biller",
                table: "Transactions",
                sql: "(\"Type\" = 'Payment'\n    AND \"BillerId\" IS NOT NULL AND \"SubscriberNo\" IS NOT NULL)\nOR (\"Type\" <> 'Payment' AND \"BillerId\" IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Billers_Name",
                table: "Billers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedPayments_BillerId",
                table: "SavedPayments",
                column: "BillerId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPayments_UserId_BillerId_SubscriberNo",
                table: "SavedPayments",
                columns: new[] { "UserId", "BillerId", "SubscriberNo" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Billers_BillerId",
                table: "Transactions",
                column: "BillerId",
                principalTable: "Billers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Billers_BillerId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "SavedPayments");

            migrationBuilder.DropTable(
                name: "Billers");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_BillerId",
                table: "Transactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transactions_Payment_Biller",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BillerId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SubscriberNo",
                table: "Transactions");
        }
    }
}
