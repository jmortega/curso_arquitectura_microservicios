using Microsoft.EntityFrameworkCore;

namespace TodoApi.Data;

// ── Entidad ───────────────────────────────────────────────────────────────────
public class Todo
{
    public int    Id    { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool   Done  { get; set; }
}

// ── DbContext ─────────────────────────────────────────────────────────────────
public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }

    public DbSet<Todo> Todos => Set<Todo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Todo>(e =>
        {
            e.ToTable("todos");
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).HasMaxLength(200).IsRequired();
            e.Property(t => t.Done).HasDefaultValue(false);
        });

        // Datos de ejemplo que se insertan en la primera migración
        modelBuilder.Entity<Todo>().HasData(
            new Todo { Id = 1, Title = "Aprender Kubernetes",   Done = false },
            new Todo { Id = 2, Title = "Desplegar con Docker",  Done = true  },
            new Todo { Id = 3, Title = "Conectar con MySQL",    Done = false }
        );
    }
}
