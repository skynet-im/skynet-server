﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SkynetServer.Entities;

namespace SkynetServer.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    partial class DatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.4-rtm-31024")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("SkynetServer.Entities.Account", b =>
                {
                    b.Property<long>("AccountId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AccountName")
                        .IsRequired();

                    b.Property<byte[]>("KeyHash")
                        .IsRequired();

                    b.HasKey("AccountId");

                    b.HasAlternateKey("AccountName");

                    b.ToTable("Accounts");
                });

            modelBuilder.Entity("SkynetServer.Entities.BlockedAccount", b =>
                {
                    b.Property<long>("OwnerId");

                    b.Property<long>("AccountId");

                    b.HasKey("OwnerId", "AccountId");

                    b.HasIndex("AccountId");

                    b.ToTable("BlockedAccount");
                });

            modelBuilder.Entity("SkynetServer.Entities.BlockedConversation", b =>
                {
                    b.Property<long>("OwnerId");

                    b.Property<long>("ChannelId");

                    b.HasKey("OwnerId", "ChannelId");

                    b.HasIndex("ChannelId");

                    b.ToTable("BlockedConversation");
                });

            modelBuilder.Entity("SkynetServer.Entities.Channel", b =>
                {
                    b.Property<long>("ChannelId");

                    b.Property<byte>("ChannelType");

                    b.Property<long?>("OtherId");

                    b.Property<long>("OwnerId");

                    b.HasKey("ChannelId");

                    b.HasIndex("OtherId");

                    b.HasIndex("OwnerId");

                    b.ToTable("Channels");
                });

            modelBuilder.Entity("SkynetServer.Entities.GroupMember", b =>
                {
                    b.Property<long>("ChannelId");

                    b.Property<long>("AccountId");

                    b.Property<byte>("Flags");

                    b.HasKey("ChannelId", "AccountId");

                    b.HasIndex("AccountId");

                    b.ToTable("GroupMember");
                });

            modelBuilder.Entity("SkynetServer.Entities.MailConfirmation", b =>
                {
                    b.Property<string>("MailAddress")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("AccountId");

                    b.Property<DateTime>("ConfirmationTime");

                    b.Property<DateTime>("CreationTime");

                    b.Property<string>("Token")
                        .IsRequired();

                    b.HasKey("MailAddress");

                    b.HasAlternateKey("Token");

                    b.HasIndex("AccountId");

                    b.ToTable("MailConfirmations");
                });

            modelBuilder.Entity("SkynetServer.Entities.Message", b =>
                {
                    b.Property<long>("ChannelId");

                    b.Property<long>("MessageId");

                    b.Property<byte[]>("ContentPacket");

                    b.Property<byte>("ContentPacketId");

                    b.Property<byte>("ContentPacketVersion");

                    b.Property<DateTime>("DispatchTime");

                    b.Property<byte>("MessageFlags");

                    b.Property<long>("SenderId");

                    b.HasKey("ChannelId", "MessageId");

                    b.HasIndex("SenderId");

                    b.ToTable("Messages");
                });

            modelBuilder.Entity("SkynetServer.Entities.MessageDependency", b =>
                {
                    b.Property<long>("AccountId");

                    b.Property<long>("ChannelId");

                    b.Property<long>("MessageId");

                    b.HasKey("AccountId", "ChannelId", "MessageId");

                    b.HasIndex("ChannelId", "MessageId");

                    b.ToTable("MessageDependencies");
                });

            modelBuilder.Entity("SkynetServer.Entities.Session", b =>
                {
                    b.Property<long>("AccountId");

                    b.Property<long>("SessionId");

                    b.Property<string>("ApplicationIdentifier")
                        .IsRequired();

                    b.Property<DateTime>("CreationTime");

                    b.Property<string>("FcmToken");

                    b.Property<DateTime>("LastConnected");

                    b.Property<int>("LastVersionCode");

                    b.HasKey("AccountId", "SessionId");

                    b.ToTable("Sessions");
                });

            modelBuilder.Entity("SkynetServer.Entities.BlockedAccount", b =>
                {
                    b.HasOne("SkynetServer.Entities.Account", "Account")
                        .WithMany("Blockers")
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("SkynetServer.Entities.Account", "Owner")
                        .WithMany("BlockedAccounts")
                        .HasForeignKey("OwnerId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("SkynetServer.Entities.BlockedConversation", b =>
                {
                    b.HasOne("SkynetServer.Entities.Channel", "Channel")
                        .WithMany("Blockers")
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("SkynetServer.Entities.Account", "Owner")
                        .WithMany("BlockedConversations")
                        .HasForeignKey("OwnerId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("SkynetServer.Entities.Channel", b =>
                {
                    b.HasOne("SkynetServer.Entities.Account", "Other")
                        .WithMany("OtherChannels")
                        .HasForeignKey("OtherId");

                    b.HasOne("SkynetServer.Entities.Account", "Owner")
                        .WithMany("OwnedChannels")
                        .HasForeignKey("OwnerId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("SkynetServer.Entities.GroupMember", b =>
                {
                    b.HasOne("SkynetServer.Entities.Account", "Account")
                        .WithMany("GroupMemberships")
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("SkynetServer.Entities.Channel", "Channel")
                        .WithMany("GroupMembers")
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("SkynetServer.Entities.MailConfirmation", b =>
                {
                    b.HasOne("SkynetServer.Entities.Account", "Account")
                        .WithMany("MailConfirmations")
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("SkynetServer.Entities.Message", b =>
                {
                    b.HasOne("SkynetServer.Entities.Channel", "Channel")
                        .WithMany("Messages")
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("SkynetServer.Entities.Account", "Sender")
                        .WithMany()
                        .HasForeignKey("SenderId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("SkynetServer.Entities.MessageDependency", b =>
                {
                    b.HasOne("SkynetServer.Entities.Message", "Message")
                        .WithMany("Dependencies")
                        .HasForeignKey("ChannelId", "MessageId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("SkynetServer.Entities.Session", b =>
                {
                    b.HasOne("SkynetServer.Entities.Account", "Account")
                        .WithMany("Sessions")
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
