using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMETA_1.Migrations
{
    /// <inheritdoc />
    public partial class WeeklyDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WeeklyAnnouncement",
                table: "Students",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeeklyAnnouncement",
                table: "Students");
        }
    }
}
