using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations.TechLead
{
    /// <inheritdoc />
    public partial class AddPasswordToTechLead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TechLeads",
                table: "TechLeads");

            migrationBuilder.RenameTable(
                name: "TechLeads",
                newName: "techleads");

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "techleads",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Token",
                table: "techleads",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_techleads",
                table: "techleads",
                column: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_techleads",
                table: "techleads");

            migrationBuilder.DropColumn(
                name: "Password",
                table: "techleads");

            migrationBuilder.DropColumn(
                name: "Token",
                table: "techleads");

            migrationBuilder.RenameTable(
                name: "techleads",
                newName: "TechLeads");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TechLeads",
                table: "TechLeads",
                column: "ID");
        }
    }
}
