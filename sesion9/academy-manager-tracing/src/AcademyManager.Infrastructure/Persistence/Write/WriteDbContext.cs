using AcademyManager.Domain.Common;
using AcademyManager.Domain.Enrollments;
using AcademyManager.Domain.Students;
using AcademyManager.Domain.Subjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AcademyManager.Infrastructure.Persistence.Write
{
    /// <summary>
    /// Write-side database context backed by PostgreSQL.
    /// After SaveChanges it dispatches accumulated domain events via MediatR.
    /// </summary>
    public sealed class WriteDbContext : DbContext
    {
        private readonly IMediator _mediator;

        public WriteDbContext(DbContextOptions<WriteDbContext> options, IMediator mediator)
            : base(options) => _mediator = mediator;

        public DbSet<Student> Students => Set<Student>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(WriteDbContext).Assembly);
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
