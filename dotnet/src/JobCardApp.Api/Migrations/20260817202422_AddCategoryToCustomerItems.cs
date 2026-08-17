using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobCardApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryToCustomerItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "CustomerItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "CustomerItems");
        }
    }
}
