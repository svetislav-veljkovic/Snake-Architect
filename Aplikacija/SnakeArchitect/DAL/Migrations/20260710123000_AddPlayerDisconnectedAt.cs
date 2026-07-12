using System;
using DAL.DataContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace DAL.Migrations
{
    [Migration("20260710123000_AddPlayerDisconnectedAt")]
    [DbContext(typeof(SnakeArchitectContext))]
    public partial class AddPlayerDisconnectedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DisconnectedAt",
                table: "Players",
                type: "timestamp with time zone",
                nullable: true);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisconnectedAt",
                table: "Players");
        }
    }
}
