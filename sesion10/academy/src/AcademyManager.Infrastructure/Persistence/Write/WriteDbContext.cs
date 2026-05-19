using AcademyManager.Domain.Common;
using AcademyManager.Domain.Enrollments;
using AcademyManager.Domain.Students;
using AcademyManager.Domain.Subjects;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AcademyManager.Infrastructure.Persistence.Write
{
    /// <summary>
    /// Write-side database context backed by PostgreSQL.
    /// After SaveChanges it dispatches accumulated domain events via MediatR.
    ///
    /// Incluye la configuración del Transactional Outbox de MassTransit:
    ///   - OutboxMessage  → eventos pendientes de publicar en RabbitMQ
    ///   - OutboxState    → estado del Worker por consumer group
    ///   - InboxState     → deduplicación de mensajes recibidos (idempotencia)
    ///
    /// Sin estas tres llamadas en OnModelCreating, EF Core no conoce los tipos
    /// y el BusOutboxDeliveryService lanza:
    ///   "Entity type not found: MassTransit.EntityFrameworkCoreIntegration.OutboxState"
    /// </summary>
    public sealed class WriteDbContext : DbContext
    {
        private readonly IMediator _mediator;

        public WriteDbContext(DbContextOptions<WriteDbContext> options, IMediator mediator)
            : base(options) => _mediator = mediator;

        public DbSet<Student>    Students    => Set<Student>();
        public DbSet<Subject>    Subjects    => Set<Subject>();
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(WriteDbContext).Assembly);

            // ── MassTransit Transactional Outbox ──────────────────────────────
            //
            //  Estas tres llamadas registran en EF Core las entidades que
            //  MassTransit necesita para el Outbox Pattern.
            //
            //  Sin ellas el BusOutboxDeliveryService falla con:
            //    "Entity type not found: OutboxState"
            //  porque EF Core no puede generar el advisory lock SQL sobre una
            //  tabla que no conoce en el modelo.
            //
            //  Tablas que EnsureCreated() / Migrate() creará en PostgreSQL:
            //    "OutboxMessage"  → eventos pendientes de enviar a RabbitMQ
            //    "OutboxState"    → lock group del Worker de entrega
            //    "InboxState"     → deduplicación (idempotencia del consumer)
            //
            modelBuilder.AddInboxStateEntity();    // tabla InboxState
            modelBuilder.AddOutboxMessageEntity(); // tabla OutboxMessage
            modelBuilder.AddOutboxStateEntity();   // tabla OutboxState

            base.OnModelCreating(modelBuilder);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            var result = await base.SaveChangesAsync(ct);
            await DispatchDomainEventsAsync(ct);
            return result;
        }

        private async Task DispatchDomainEventsAsync(CancellationToken ct)
        {
            var entities = ChangeTracker.Entries<Entity<StudentId>>()
                .Select(e => e.Entity)
                .Cast<object>()
                .Concat(ChangeTracker.Entries<Entity<SubjectId>>().Select(e => e.Entity))
                .Concat(ChangeTracker.Entries<Entity<EnrollmentId>>().Select(e => e.Entity))
                .ToList();

            var domainEvents = entities
                .OfType<dynamic>()
                .SelectMany((dynamic e) => (IReadOnlyList<IDomainEvent>)e.DomainEvents)
                .ToList();

            foreach (dynamic entity in entities)
                entity.ClearDomainEvents();

            foreach (var domainEvent in domainEvents)
                await _mediator.Publish(domainEvent, ct);
        }
    }
}
