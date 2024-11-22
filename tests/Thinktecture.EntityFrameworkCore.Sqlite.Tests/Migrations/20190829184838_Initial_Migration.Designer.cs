﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Migrations
{
    [DbContext(typeof(TestDbContext))]
    [Migration("20190829184838_Initial_Migration")]
    partial class Initial_Migration
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079");

            modelBuilder.Entity("Thinktecture.TestDatabaseContext.TestEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Count");

                    b.Property<string>("Name");

                    b.Property<int>("PropertyWithBackingField");

                    b.Property<int>("_privateField");

                    b.HasKey("Id");

                    b.ToTable("TestEntities");
                });

            modelBuilder.Entity("Thinktecture.TestDatabaseContext.TestEntityWithAutoIncrement", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.ToTable("TestEntitiesWithAutoIncrement");
                });

            modelBuilder.Entity("Thinktecture.TestDatabaseContext.TestEntityWithShadowProperties", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.Property<int?>("ShadowIntProperty");

                    b.Property<string>("ShadowStringProperty")
                        .HasMaxLength(50);

                    b.HasKey("Id");

                    b.ToTable("TestEntitiesWithShadowProperties");
                });
#pragma warning restore 612, 618
        }
    }
}
