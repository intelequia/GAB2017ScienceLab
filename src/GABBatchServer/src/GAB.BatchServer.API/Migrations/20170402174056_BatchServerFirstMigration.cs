using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GAB.BatchServer.API.Migrations
{
    /// <summary>
    /// First Entity Framework migration of the database
    /// </summary>
    public partial class BatchServerFirstMigration : Migration
    {
        /// <summary>
        /// Step UP on the migration
        /// </summary>
        /// <param name="migrationBuilder"></param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LabUsers",
                columns: table => new
                {
                    LabUserId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CompanyName = table.Column<string>(maxLength: 50, nullable: true),
                    CountryCode = table.Column<string>(maxLength: 2, nullable: true),
                    CreatedOn = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                    EMail = table.Column<string>(maxLength: 100, nullable: true),
                    FullName = table.Column<string>(maxLength: 50, nullable: true),
                    Location = table.Column<string>(maxLength: 50, nullable: true),
                    ModifiedOn = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                    TeamName = table.Column<string>(maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabUsers", x => x.LabUserId);
                });

            migrationBuilder.CreateTable(
                name: "Inputs",
                columns: table => new
                {
                    InputId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    AssignedToLabUserId = table.Column<int>(nullable: true),
                    BatchId = table.Column<Guid>(nullable: true),
                    CreatedOn = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                    ModifiedOn = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                    Parameters = table.Column<string>(maxLength: 800, nullable: true),
                    Status = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inputs", x => x.InputId);
                    table.ForeignKey(
                        name: "FK_Inputs_LabUsers_AssignedToLabUserId",
                        column: x => x.AssignedToLabUserId,
                        principalTable: "LabUsers",
                        principalColumn: "LabUserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Outputs",
                columns: table => new
                {
                    OutputId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    AvgScore = table.Column<double>(nullable: false),
                    CreatedOn = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                    InputId = table.Column<int>(nullable: true),
                    MaxScore = table.Column<double>(nullable: false),
                    ModifiedOn = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                    Result = table.Column<string>(maxLength: 512, nullable: true),
                    TotalItems = table.Column<int>(nullable: false),
                    TotalScore = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outputs", x => x.OutputId);
                    table.ForeignKey(
                        name: "FK_Outputs_Inputs_InputId",
                        column: x => x.InputId,
                        principalTable: "Inputs",
                        principalColumn: "InputId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inputs_AssignedToLabUserId",
                table: "Inputs",
                column: "AssignedToLabUserId");

            migrationBuilder.CreateIndex(
                name: "IDX_BatchId",
                table: "Inputs",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IDX_Status",
                table: "Inputs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IDX_Email",
                table: "LabUsers",
                column: "EMail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Outputs_InputId",
                table: "Outputs",
                column: "InputId");
        }

        /// <summary>
        /// Step down on the migration
        /// </summary>
        /// <param name="migrationBuilder"></param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Outputs");

            migrationBuilder.DropTable(
                name: "Inputs");

            migrationBuilder.DropTable(
                name: "LabUsers");
        }
    }
}
