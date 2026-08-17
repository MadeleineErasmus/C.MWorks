using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobCardApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteToJobCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SiteId",
                table: "JobCards",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobCards_SiteId",
                table: "JobCards",
                column: "SiteId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobCards_CustomerSites_SiteId",
                table: "JobCards",
                column: "SiteId",
                principalTable: "CustomerSites",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobCards_CustomerSites_SiteId",
                table: "JobCards");

            migrationBuilder.DropIndex(
                name: "IX_JobCards_SiteId",
                table: "JobCards");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "JobCards");
        }
    }
}
