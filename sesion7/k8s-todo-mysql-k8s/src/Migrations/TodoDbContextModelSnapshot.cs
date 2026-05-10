using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TodoApi.Data;

#nullable disable

namespace TodoApi.Migrations;

[DbContext(typeof(TodoDbContext))]
partial class TodoDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.0")
            .HasAnnotation("Relational:MaxIdentifierLength", 64);

        modelBuilder.Entity("TodoApi.Data.Todo", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int");

            b.Property<bool>("Done")
                .HasDefaultValue(false)
                .HasColumnType("tinyint(1)");

            b.Property<string>("Title")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("varchar(200)");

            b.HasKey("Id");
            b.ToTable("todos");

            b.HasData(
                new { Id = 1, Done = false, Title = "Aprender Kubernetes"  },
                new { Id = 2, Done = true,  Title = "Desplegar con Docker" },
                new { Id = 3, Done = false, Title = "Conectar con MySQL"   });
        });
#pragma warning restore 612, 618
    }
}
