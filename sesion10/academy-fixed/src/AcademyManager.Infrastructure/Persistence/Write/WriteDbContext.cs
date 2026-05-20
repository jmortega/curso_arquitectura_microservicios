using AcademyManager.Domain.Common;
using AcademyManager.Domain.Enrollments;
using AcademyManager.Domain.Students;
using AcademyManager.Domain.Subjects;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AcademyManager.Infrastructure.Persistence.Write
{
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
            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();
            base.OnModelCreating(modelBuilder);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            // ── ORDEN CRÍTICO PARA EL OUTBOX PATTERN ─────────────────────────
            //
            //  Los DomainEventHandlers llaman a IPublishEndpoint.Publish() que,
            //  gracias al UseBusOutbox(), escribe el mensaje en la tabla
            //  OutboxMessage DENTRO de la transacción de EF Core.
            //
            //  Si se invierte el orden (SaveChanges → Dispatch), la transacción
            //  ya está cerrada cuando se llama a Publish() y el mensaje no puede
            //  participar en ella → OutboxMessage nunca recibe filas.
            //
            //  CORRECTO:  Dispatch (escribe OutboxMessage) → SaveChanges (commit todo junto)
            //  INCORRECTO: SaveChanges (commit) → Dispatch (fuera de transacción)
            //
            await DispatchDomainEventsAsync(ct);      // 1. publica en Outbox (dentro de tx)
            return await base.SaveChangesAsync(ct);   // 2. commit: datos + OutboxMessage juntos
        }

        private async Task DispatchDomainEventsAsync(CancellationToken ct)
        {
            // Recoger eventos ANTES del SaveChanges: el ChangeTracker aún tiene
            // las entidades en estado Added/Modified con sus domain events.
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

            // Limpiar antes de publicar para evitar doble dispatch si SaveChanges reintenta
            foreach (dynamic entity in entities)
                entity.ClearDomainEvents();

            foreach (var domainEvent in domainEvents)
                await _mediator.Publish(domainEvent, ct);
        }
    }
}
