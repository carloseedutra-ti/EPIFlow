using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EPIFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaGestaoMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeLimit",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuspended",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Tenants",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionExpiresOnUtc",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspendedAtUtc",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TenantPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantPayments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantPayments_TenantId",
                table: "TenantPayments",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantPayments");

            migrationBuilder.DropColumn(
                name: "EmployeeLimit",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsSuspended",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SubscriptionExpiresOnUtc",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SuspendedAtUtc",
                table: "Tenants");
        }
    }
}
