using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniShare.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 删除DisplayName列
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "AspNetUsers");

            // 添加Birthday列
            migrationBuilder.AddColumn<DateTime>(
                name: "Birthday",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: true);

            // 添加Gender列
            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // 添加Major列
            migrationBuilder.AddColumn<string>(
                name: "Major",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 恢复DisplayName列
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // 删除Birthday列
            migrationBuilder.DropColumn(
                name: "Birthday",
                table: "AspNetUsers");

            // 删除Gender列
            migrationBuilder.DropColumn(
                name: "Gender",
                table: "AspNetUsers");

            // 删除Major列
            migrationBuilder.DropColumn(
                name: "Major",
                table: "AspNetUsers");
        }
    }
}