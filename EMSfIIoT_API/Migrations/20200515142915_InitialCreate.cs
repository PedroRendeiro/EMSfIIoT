using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EMSfIIoT_API.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Event",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<Guid>(nullable: false),
                    Variable = table.Column<string>(nullable: true),
                    EventType = table.Column<int>(nullable: false),
                    EventValueType = table.Column<int>(nullable: false),
                    EventValue = table.Column<long>(nullable: false),
                    EventFrequency = table.Column<DateTime>(nullable: false),
                    NotificationType = table.Column<int>(nullable: false),
                    StartSubscriptionDate = table.Column<DateTime>(nullable: false),
                    EndSubscriptionDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.Id);
                });
        }

        /*
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Event");

            migrationBuilder.DropTable(
                name: "Measure_API");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Users");
        }
        */
    }
}
