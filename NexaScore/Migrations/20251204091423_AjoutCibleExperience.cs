using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projet.Migrations
{
    /// <inheritdoc />
    public partial class AjoutCibleExperience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "PoidsLocalisation",
                table: "ParametreScoring",
                type: "int",
                nullable: false,
                defaultValue: 20,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldDefaultValue: 20);

            migrationBuilder.AlterColumn<int>(
                name: "PoidsExperience",
                table: "ParametreScoring",
                type: "int",
                nullable: false,
                defaultValue: 30,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldDefaultValue: 30);

            migrationBuilder.AlterColumn<int>(
                name: "PoidsCompetences",
                table: "ParametreScoring",
                type: "int",
                nullable: false,
                defaultValue: 50,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldDefaultValue: 50);

            migrationBuilder.AlterColumn<bool>(
                name: "ExclureSiVilleDiff",
                table: "ParametreScoring",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true,
                oldDefaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CibleExperience",
                table: "ParametreScoring",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CibleExperience",
                table: "ParametreScoring");

            migrationBuilder.AlterColumn<int>(
                name: "PoidsLocalisation",
                table: "ParametreScoring",
                type: "int",
                nullable: true,
                defaultValue: 20,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 20);

            migrationBuilder.AlterColumn<int>(
                name: "PoidsExperience",
                table: "ParametreScoring",
                type: "int",
                nullable: true,
                defaultValue: 30,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 30);

            migrationBuilder.AlterColumn<int>(
                name: "PoidsCompetences",
                table: "ParametreScoring",
                type: "int",
                nullable: true,
                defaultValue: 50,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 50);

            migrationBuilder.AlterColumn<bool>(
                name: "ExclureSiVilleDiff",
                table: "ParametreScoring",
                type: "bit",
                nullable: true,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);
        }
    }
}
