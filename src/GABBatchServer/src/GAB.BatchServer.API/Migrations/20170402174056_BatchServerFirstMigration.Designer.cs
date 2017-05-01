using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using GAB.BatchServer.API.Data;

namespace GAB.BatchServer.API.Migrations
{
    [DbContext(typeof(BatchServerContext))]
    [Migration("20170402174056_BatchServerFirstMigration")]
    partial class BatchServerFirstMigration
    {
        /// <summary>
        /// Builds the target model
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.1")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("GAB.BatchServer.API.Models.Input", b =>
                {
                    b.Property<int>("InputId")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("AssignedToLabUserId");

                    b.Property<Guid?>("BatchId");

                    b.Property<DateTime>("CreatedOn")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("getutcdate()");

                    b.Property<DateTime>("ModifiedOn")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("getutcdate()");

                    b.Property<string>("Parameters")
                        .HasMaxLength(800);

                    b.Property<int>("Status");

                    b.HasKey("InputId");

                    b.HasIndex("AssignedToLabUserId");

                    b.HasIndex("BatchId")
                        .HasName("IDX_BatchId");

                    b.HasIndex("Status")
                        .HasName("IDX_Status");

                    b.ToTable("Inputs");
                });

            modelBuilder.Entity("GAB.BatchServer.API.Models.LabUser", b =>
                {
                    b.Property<int>("LabUserId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CompanyName")
                        .HasMaxLength(50);

                    b.Property<string>("CountryCode")
                        .HasMaxLength(2);

                    b.Property<DateTime>("CreatedOn")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("getutcdate()");

                    b.Property<string>("EMail")
                        .HasMaxLength(100);

                    b.Property<string>("FullName")
                        .HasMaxLength(50);

                    b.Property<string>("Location")
                        .HasMaxLength(50);

                    b.Property<DateTime>("ModifiedOn")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("getutcdate()");

                    b.Property<string>("TeamName")
                        .HasMaxLength(100);

                    b.HasKey("LabUserId");

                    b.HasIndex("EMail")
                        .IsUnique()
                        .HasName("IDX_Email");

                    b.ToTable("LabUsers");
                });

            modelBuilder.Entity("GAB.BatchServer.API.Models.Output", b =>
                {
                    b.Property<int>("OutputId")
                        .ValueGeneratedOnAdd();

                    b.Property<double>("AvgScore");

                    b.Property<DateTime>("CreatedOn")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("getutcdate()");

                    b.Property<int?>("InputId");

                    b.Property<double>("MaxScore");

                    b.Property<DateTime>("ModifiedOn")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("getutcdate()");

                    b.Property<string>("Result")
                        .HasMaxLength(512);

                    b.Property<int>("TotalItems");

                    b.Property<double>("TotalScore");

                    b.HasKey("OutputId");

                    b.HasIndex("InputId");

                    b.ToTable("Outputs");
                });

            modelBuilder.Entity("GAB.BatchServer.API.Models.Input", b =>
                {
                    b.HasOne("GAB.BatchServer.API.Models.LabUser", "AssignedTo")
                        .WithMany()
                        .HasForeignKey("AssignedToLabUserId");
                });

            modelBuilder.Entity("GAB.BatchServer.API.Models.Output", b =>
                {
                    b.HasOne("GAB.BatchServer.API.Models.Input", "Input")
                        .WithMany()
                        .HasForeignKey("InputId");
                });
        }
    }
}
