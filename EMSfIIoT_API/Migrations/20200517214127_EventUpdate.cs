using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EMSfIIoT_API.Migrations
{
    public partial class EventUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "EventFrequency",
                table: "Event",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "EventFrequency",
                table: "Event",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(int));
        }
    }
}
