using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvReader.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FolderId",
                table: "Profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Folders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Folders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Folders_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_FolderId",
                table: "Profiles",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_OwnerId_Name",
                table: "Folders",
                columns: new[] { "OwnerId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Profiles_Folders_FolderId",
                table: "Profiles",
                column: "FolderId",
                principalTable: "Folders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Profiles_Folders_FolderId",
                table: "Profiles");

            migrationBuilder.DropTable(
                name: "Folders");

            migrationBuilder.DropIndex(
                name: "IX_Profiles_FolderId",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "Profiles");
        }
    }
}
