using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Telegram.Bot.Advanced.Test.Migrations
{
    public partial class UsedRightProject : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Data_Users_UserId",
                table: "Data");

            migrationBuilder.DropForeignKey(
                name: "FK_Masters_Users_UserId",
                table: "Masters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Data",
                table: "Data");

            migrationBuilder.DropIndex(
                name: "IX_Data_UserId",
                table: "Data");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "Masters",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "Data",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Data_Key_UserId",
                table: "Data",
                columns: new[] { "Key", "UserId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Data",
                table: "Data",
                columns: new[] { "UserId", "Key" });

            migrationBuilder.AddForeignKey(
                name: "FK_Data_Users_UserId",
                table: "Data",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Masters_Users_UserId",
                table: "Masters",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Data_Users_UserId",
                table: "Data");

            migrationBuilder.DropForeignKey(
                name: "FK_Masters_Users_UserId",
                table: "Masters");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Data_Key_UserId",
                table: "Data");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Data",
                table: "Data");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "Masters",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "Data",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Data",
                table: "Data",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "IX_Data_UserId",
                table: "Data",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Data_Users_UserId",
                table: "Data",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Masters_Users_UserId",
                table: "Masters",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
