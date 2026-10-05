using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace CvReader.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnershipAndLiveMatching : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchResults");

            // Sahibi bilinmeyen eski kayıtlar yeni OwnerId kısıtını karşılayamaz; vektörleri cascade ile silinir.
            migrationBuilder.Sql("""DELETE FROM "Profiles"; DELETE FROM "JobPostings";""");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Profiles");

            migrationBuilder.AddColumn<int>(
                name: "TokenVersion",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "Profiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Profiles",
                type: "uuid",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "JobPostings",
                type: "uuid",
                nullable: false);

            migrationBuilder.CreateTable(
                name: "JobPostingEmbeddings",
                columns: table => new
                {
                    JobPostingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1024)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPostingEmbeddings", x => x.JobPostingId);
                    table.ForeignKey(
                        name: "FK_JobPostingEmbeddings_JobPostings_JobPostingId",
                        column: x => x.JobPostingId,
                        principalTable: "JobPostings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_OwnerId_ContentHash",
                table: "Profiles",
                columns: new[] { "OwnerId", "ContentHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_OwnerId_CreatedAt",
                table: "JobPostings",
                columns: new[] { "OwnerId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_JobPostings_Users_OwnerId",
                table: "JobPostings",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Profiles_Users_OwnerId",
                table: "Profiles",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobPostings_Users_OwnerId",
                table: "JobPostings");

            migrationBuilder.DropForeignKey(
                name: "FK_Profiles_Users_OwnerId",
                table: "Profiles");

            migrationBuilder.DropTable(
                name: "JobPostingEmbeddings");

            migrationBuilder.DropIndex(
                name: "IX_Profiles_OwnerId_ContentHash",
                table: "Profiles");

            migrationBuilder.DropIndex(
                name: "IX_JobPostings_OwnerId_CreatedAt",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "TokenVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "JobPostings");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Profiles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Profiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MatchResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobPostingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchResults_JobPostings_JobPostingId",
                        column: x => x.JobPostingId,
                        principalTable: "JobPostings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchResults_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_JobPostingId_ProfileId",
                table: "MatchResults",
                columns: new[] { "JobPostingId", "ProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_ProfileId",
                table: "MatchResults",
                column: "ProfileId");
        }
    }
}
