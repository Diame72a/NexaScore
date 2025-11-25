using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projet.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Competence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Competen__3214EC077FB5CF27", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Personne",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Prenom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DateNaissance = table.Column<DateOnly>(type: "date", nullable: false),
                    Ville = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CodePostal = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    AnneesExperienceTotal = table.Column<int>(type: "int", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Personne__3214EC07C70B9BFC", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Poste",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Intitule = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Poste__3214EC07DF0C6F2E", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompetenceAcquise",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonneId = table.Column<int>(type: "int", nullable: false),
                    CompetenceId = table.Column<int>(type: "int", nullable: false),
                    Niveau = table.Column<int>(type: "int", nullable: true, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Competen__3214EC076EFB2F5B", x => x.Id);
                    table.ForeignKey(
                        name: "FK__Competenc__Compe__5DCAEF64",
                        column: x => x.CompetenceId,
                        principalTable: "Competence",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK__Competenc__Perso__5CD6CB2B",
                        column: x => x.PersonneId,
                        principalTable: "Personne",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Offre",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    VilleCible = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CodePostalCible = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PosteId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Offre__3214EC07C6FDC028", x => x.Id);
                    table.ForeignKey(
                        name: "FK__Offre__PosteId__5070F446",
                        column: x => x.PosteId,
                        principalTable: "Poste",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CompetenceSouhaitee",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OffreId = table.Column<int>(type: "int", nullable: false),
                    CompetenceId = table.Column<int>(type: "int", nullable: false),
                    NiveauRequis = table.Column<int>(type: "int", nullable: true, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Competen__3214EC07CE71D8DB", x => x.Id);
                    table.ForeignKey(
                        name: "FK__Competenc__Compe__59063A47",
                        column: x => x.CompetenceId,
                        principalTable: "Competence",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK__Competenc__Offre__5812160E",
                        column: x => x.OffreId,
                        principalTable: "Offre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParametreScoring",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OffreId = table.Column<int>(type: "int", nullable: false),
                    PoidsCompetences = table.Column<int>(type: "int", nullable: true, defaultValue: 50),
                    PoidsExperience = table.Column<int>(type: "int", nullable: true, defaultValue: 30),
                    PoidsLocalisation = table.Column<int>(type: "int", nullable: true, defaultValue: 20),
                    ExclureSiVilleDiff = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    ExclureSiExperienceManquante = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Parametr__3214EC0790BD927F", x => x.Id);
                    table.ForeignKey(
                        name: "FK__Parametre__Offre__66603565",
                        column: x => x.OffreId,
                        principalTable: "Offre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UQ__Competen__C7D1C61E70A7CAB6",
                table: "Competence",
                column: "Nom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompetenceAcquise_CompetenceId",
                table: "CompetenceAcquise",
                column: "CompetenceId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetenceAcquise_PersonneId",
                table: "CompetenceAcquise",
                column: "PersonneId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetenceSouhaitee_CompetenceId",
                table: "CompetenceSouhaitee",
                column: "CompetenceId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetenceSouhaitee_OffreId",
                table: "CompetenceSouhaitee",
                column: "OffreId");

            migrationBuilder.CreateIndex(
                name: "IX_Offre_PosteId",
                table: "Offre",
                column: "PosteId");

            migrationBuilder.CreateIndex(
                name: "UQ__Parametr__540A13133B54325B",
                table: "ParametreScoring",
                column: "OffreId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Personne__A9D10534B1605CA4",
                table: "Personne",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompetenceAcquise");

            migrationBuilder.DropTable(
                name: "CompetenceSouhaitee");

            migrationBuilder.DropTable(
                name: "ParametreScoring");

            migrationBuilder.DropTable(
                name: "Personne");

            migrationBuilder.DropTable(
                name: "Competence");

            migrationBuilder.DropTable(
                name: "Offre");

            migrationBuilder.DropTable(
                name: "Poste");
        }
    }
}
