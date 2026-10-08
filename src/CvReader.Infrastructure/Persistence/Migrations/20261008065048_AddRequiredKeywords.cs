using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvReader.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRequiredKeywords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "RequiredKeywords",
                table: "JobPostings",
                type: "varchar(100)[]",
                nullable: false,
                // Mevcut ilanların zorunlu anahtar kelimesi yoktur.
                defaultValueSql: "'{}'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiredKeywords",
                table: "JobPostings");
        }
    }
}
