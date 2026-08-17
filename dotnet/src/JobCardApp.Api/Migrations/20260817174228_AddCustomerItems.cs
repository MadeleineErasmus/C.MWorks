using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobCardApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerItemId",
                table: "JobCardLines",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerItems_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobCardLines_CustomerItemId",
                table: "JobCardLines",
                column: "CustomerItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerItems_CustomerId_Name",
                table: "CustomerItems",
                columns: new[] { "CustomerId", "Name" });

            migrationBuilder.AddForeignKey(
                name: "FK_JobCardLines_CustomerItems_CustomerItemId",
                table: "JobCardLines",
                column: "CustomerItemId",
                principalTable: "CustomerItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobCardLines_CustomerItems_CustomerItemId",
                table: "JobCardLines");

            migrationBuilder.DropTable(
                name: "CustomerItems");

            migrationBuilder.DropIndex(
                name: "IX_JobCardLines_CustomerItemId",
                table: "JobCardLines");

            migrationBuilder.DropColumn(
                name: "CustomerItemId",
                table: "JobCardLines");
        }
    }
}
