using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobCardApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUsername : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Backfill existing users (who previously logged in with Email)
            // with a derived Username before the unique index is enforced,
            // so this migration doesn't break accounts that already exist.
            migrationBuilder.Sql(@"
                UPDATE Users
                SET Username = LOWER(LEFT(Email, CHARINDEX('@', Email) - 1))
                WHERE Email IS NOT NULL AND CHARINDEX('@', Email) > 1;

                UPDATE Users
                SET Username = CONCAT('user', Id)
                WHERE Username IS NULL OR Username = '';

                ;WITH ranked AS (
                    SELECT Id, Username, ROW_NUMBER() OVER (PARTITION BY Username ORDER BY Id) AS rn
                    FROM Users
                )
                UPDATE u SET u.Username = u.Username + CAST(r.rn - 1 AS VARCHAR)
                FROM Users u JOIN ranked r ON u.Id = r.Id
                WHERE r.rn > 1;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }
    }
}
