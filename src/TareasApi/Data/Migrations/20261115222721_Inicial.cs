using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TareasApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tareas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Responsable = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FechaLimite = table.Column<DateOnly>(type: "date", nullable: false),
                    CreadaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualizadaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tareas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tareas_Estado",
                table: "tareas",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_tareas_Responsable",
                table: "tareas",
                column: "Responsable");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tareas");
        }
    }
}
