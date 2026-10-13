using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotikaIdentityEmail.Migrations
{
    /// <inheritdoc />
    public partial class sendDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SenDate",
                table: "Messages",
                newName: "SendDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SendDate",
                table: "Messages",
                newName: "SenDate");
        }
    }
}
