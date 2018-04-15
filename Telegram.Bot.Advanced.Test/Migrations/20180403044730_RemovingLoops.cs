using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Telegram.Bot.Advanced.Test.Migrations
{
    public partial class RemovingLoops : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RegisteredChats_Users_ChatId",
                table: "RegisteredChats");

            migrationBuilder.DropForeignKey(
                name: "FK_RegisteredChats_Masters_MasterId",
                table: "RegisteredChats");

            migrationBuilder.AddForeignKey(
                name: "FK_RegisteredChats_Users_ChatId",
                table: "RegisteredChats",
                column: "ChatId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RegisteredChats_Masters_MasterId",
                table: "RegisteredChats",
                column: "MasterId",
                principalTable: "Masters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RegisteredChats_Users_ChatId",
                table: "RegisteredChats");

            migrationBuilder.DropForeignKey(
                name: "FK_RegisteredChats_Masters_MasterId",
                table: "RegisteredChats");

            migrationBuilder.AddForeignKey(
                name: "FK_RegisteredChats_Users_ChatId",
                table: "RegisteredChats",
                column: "ChatId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RegisteredChats_Masters_MasterId",
                table: "RegisteredChats",
                column: "MasterId",
                principalTable: "Masters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
