using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projet.Migrations
{
    /// <inheritdoc />
    public partial class AjoutFichiersUpload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CvNomFichier",
                table: "Personne",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CvPath",
                table: "Personne",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageBannierePath",
                table: "Personne",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageProfilPath",
                table: "Personne",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CvNomFichier",
                table: "Personne");

            migrationBuilder.DropColumn(
                name: "CvPath",
                table: "Personne");

            migrationBuilder.DropColumn(
                name: "ImageBannierePath",
                table: "Personne");

            migrationBuilder.DropColumn(
                name: "ImageProfilPath",
                table: "Personne");
        }
    }
}
