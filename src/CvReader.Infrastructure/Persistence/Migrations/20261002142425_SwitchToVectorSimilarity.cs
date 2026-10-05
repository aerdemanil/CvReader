using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace CvReader.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SwitchToVectorSimilarity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchedKeywords",
                table: "MatchResults");

            migrationBuilder.DropColumn(
                name: "MissingKeywords",
                table: "MatchResults");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "MatchResults");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfileEmbeddings");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddColumn<List<string>>(
                name: "MatchedKeywords",
                table: "MatchResults",
                type: "varchar(100)[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "MissingKeywords",
                table: "MatchResults",
                type: "varchar(100)[]",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "Tier",
                table: "MatchResults",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");
        }
    }
}
