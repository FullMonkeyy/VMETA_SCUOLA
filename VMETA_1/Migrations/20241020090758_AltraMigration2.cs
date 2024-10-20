using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMETA_1.Migrations
{
    /// <inheritdoc />
    public partial class AltraMigration2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "YEARAuthor",
                table: "Letters",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClassroomYEAR",
                table: "Announcements",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YEARAuthor",
                table: "Letters");

            migrationBuilder.DropColumn(
                name: "ClassroomYEAR",
                table: "Announcements");
        }
    }
}
