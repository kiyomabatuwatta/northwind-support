using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SupportDesk.Infrastructure.Data;

#nullable disable

namespace SupportDesk.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Adds the credential column to Agents. Nullable: existing agents stay valid and simply
    /// cannot sign in until a hash is set (the development seed sets one).
    /// </summary>
    [DbContext(typeof(SupportDbContext))]
    [Migration("20261021210000_AddAgentPasswordHash")]
    public partial class AddAgentPasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Agents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Agents");
        }
    }
}
