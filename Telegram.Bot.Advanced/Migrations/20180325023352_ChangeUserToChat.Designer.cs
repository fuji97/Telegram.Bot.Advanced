﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Migrations
{
    [DbContext(typeof(UserContext))]
    [Migration("20180325023352_ChangeUserToChat")]
    partial class ChangeUserToChat
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.0.2-rtm-10011")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Telegram.Bot.Advanced.Models.Data", b =>
                {
                    b.Property<string>("Key")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("UserId");

                    b.Property<string>("Value");

                    b.HasKey("Key");

                    b.HasIndex("UserId");

                    b.ToTable("Data");
                });

            modelBuilder.Entity("Telegram.Bot.Advanced.Models.TelegramChat", b =>
                {
                    b.Property<long>("Id");

                    b.Property<bool>("AllMembersAreAdministrators");

                    b.Property<bool?>("CanSetStickerSet");

                    b.Property<string>("Description");

                    b.Property<string>("FirstName");

                    b.Property<string>("InviteLink");

                    b.Property<string>("LastName");

                    b.Property<int>("Role");

                    b.Property<int>("State");

                    b.Property<string>("StickerSetName");

                    b.Property<string>("Title");

                    b.Property<int>("Type");

                    b.Property<string>("Username");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Telegram.Bot.Advanced.Models.Data", b =>
                {
                    b.HasOne("Telegram.Bot.Advanced.Models.TelegramChat", "User")
                        .WithMany("Data")
                        .HasForeignKey("UserId");
                });
#pragma warning restore 612, 618
        }
    }
}