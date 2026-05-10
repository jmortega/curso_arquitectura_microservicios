using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApi.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "todos",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Title = table.Column<string>(maxLength: 200, nullable: false),
                Done  = table.Column<bool>(nullable: false, defaultValue: false)
            },
            constraints: table => table.PrimaryKey("PK_todos", x => x.Id))
            .Annotation("MySql:CharSet", "utf8mb4");

        // Seed data
        migrationBuilder.InsertData(
            table: "todos",
            columns: new[] { "Id", "Title", "Done" },
            values: new object[,]
            {
                { 1, "Aprender Kubernetes",  false },
                { 2, "Desplegar con Docker", true  },
                { 3, "Conectar con MySQL",   false }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "todos");
    }
}
