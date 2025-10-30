using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EPIFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjustaForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Tenants",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressComplement",
                table: "Tenants",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Tenants",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Tenants",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Tenants",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsibleEmail",
                table: "Tenants",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsibleName",
                table: "Tenants",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Tenants",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressComplement",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ResponsibleEmail",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ResponsibleName",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Tenants");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true);
        }
    }
}
