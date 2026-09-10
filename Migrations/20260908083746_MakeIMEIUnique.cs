using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MobileTracker.Migrations
{
    /// <inheritdoc />
    public partial class MakeIMEIUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_LostPhoneReports_IMEI",
                table: "LostPhoneReports",
                column: "IMEI",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LostPhoneReports_IMEI",
                table: "LostPhoneReports");
        }
    }
}
