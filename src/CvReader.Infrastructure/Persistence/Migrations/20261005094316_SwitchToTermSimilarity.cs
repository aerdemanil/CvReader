using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace CvReader.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SwitchToTermSimilarity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobPostingEmbeddings");

            migrationBuilder.DropTable(
                name: "ProfileEmbeddings");

            // Terim vektörleri migration içinde üretilemez; eski CV'ler ve ilanlar skorlanamayacağı için silinir.
            migrationBuilder.Sql("""DELETE FROM "Profiles"; DELETE FROM "JobPostings";""");

            migrationBuilder.CreateTable(
                name: "JobPostingTerms",
                columns: table => new
                {
                    JobPostingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Term = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1024)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPostingTerms", x => new { x.JobPostingId, x.Term });
                    table.ForeignKey(
                        name: "FK_JobPostingTerms_JobPostings_JobPostingId",
                        column: x => x.JobPostingId,
                        principalTable: "JobPostings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfileTerms",
                columns: table => new
                {
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Term = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1024)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileTerms", x => new { x.ProfileId, x.Term });
                    table.ForeignKey(
                        name: "FK_ProfileTerms_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobPostingTerms");

            migrationBuilder.DropTable(
                name: "ProfileTerms");

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

            migrationBuilder.CreateTable(
                name: "ProfileEmbeddings",
                columns: table => new
                {
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1024)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileEmbeddings", x => x.ProfileId);
                    table.ForeignKey(
                        name: "FK_ProfileEmbeddings_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
