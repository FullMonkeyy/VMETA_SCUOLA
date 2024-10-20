using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMETA_1.Migrations
{
    /// <inheritdoc />
    public partial class IMMYGRATTI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InsertionDate",
                table: "Letters",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InsertionDate",
                table: "Letters");
        }
    }
}
