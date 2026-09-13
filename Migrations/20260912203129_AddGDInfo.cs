using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MobileTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddGDInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GDInfos",
                columns: table => new
                {
                    GDInfoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GDNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ThanaId = table.Column<int>(type: "integer", nullable: false),
                    GDPhotoPath = table.Column<string>(type: "text", nullable: true),
                    CaseId = table.Column<int>(type: "integer", nullable: false),
                    LostPhoneReportCaseId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GDInfos", x => x.GDInfoId);
                    table.ForeignKey(
                        name: "FK_GDInfos_LostPhoneReports_LostPhoneReportCaseId",
                        column: x => x.LostPhoneReportCaseId,
                        principalTable: "LostPhoneReports",
                        principalColumn: "CaseId");
                    table.ForeignKey(
                        name: "FK_GDInfos_Thanas_ThanaId",
                        column: x => x.ThanaId,
                        principalTable: "Thanas",
                        principalColumn: "ThanaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GDInfos_LostPhoneReportCaseId",
                table: "GDInfos",
                column: "LostPhoneReportCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_GDInfos_ThanaId",
                table: "GDInfos",
                column: "ThanaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GDInfos");
        }
    }
}
