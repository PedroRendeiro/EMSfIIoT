using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EMSfIIoT_API.Migrations
{
    public partial class EventUpdateActive : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Measure_API");

            migrationBuilder.DropColumn(
                name: "EndSubscriptionDate",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "StartSubscriptionDate",
                table: "Event");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdate",
                table: "Event",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "OnHold",
                table: "Event",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdate",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "OnHold",
                table: "Event");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndSubscriptionDate",
                table: "Event",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartSubscriptionDate",
                table: "Event",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "Measure_API",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationID = table.Column<long>(type: "bigint", nullable: false),
                    MeasureTypeID = table.Column<int>(type: "int", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Measure_API", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Measure_API_Id",
                table: "Measure_API",
                column: "Id",
                unique: true);
        }
    }
}
