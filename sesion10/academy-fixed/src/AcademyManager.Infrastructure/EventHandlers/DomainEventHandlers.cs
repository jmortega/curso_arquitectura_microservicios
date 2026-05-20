using AcademyManager.Application.Common.Interfaces;
using AcademyManager.Application.ReadModels;
using AcademyManager.Domain.Students.Events;
using AcademyManager.Domain.Subjects.Events;
using MediatR;

namespace AcademyManager.Infrastructure.EventHandlers
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  IMPORTANTE — Separación de responsabilidades entre handlers:
    //
    //  ✅ ESTOS handlers (EventHandlers/) actualizan MongoDB directamente.
    //     Se usan para eventos que NO pasan por RabbitMQ porque son operaciones
    //     simples de proyección sin necesidad de garantías de entrega asíncrona.
    //
    //  ❌ ELIMINADOS de este fichero:
    //     - StudentCreatedEventHandler  → ahora lo gestiona StudentCreatedConsumer
    //       (MassTransit) para garantizar At-least-once delivery via Outbox.
    //     - EnrollmentCreatedEventHandler → ahora lo gestiona AlumnoMatriculadoConsumer
    //       (MassTransit). Tenerlo aquí Y en MassTransit causaba que MongoDB se
    //       actualizara directamente (cortocircuitando el Outbox) y los mensajes
    //       nunca llegaban a RabbitMQ ni a la tabla OutboxMessage.
    //
    //  Los handlers que sí permanecen aquí son actualizaciones y borrados que
    //  no tienen consumer dedicado en MassTransit.
    // ═══════════════════════════════════════════════════════════════════════════

    // ── Estudiante actualizado → actualiza MongoDB directamente ──────────────
    internal sealed class StudentUpdatedEventHandler
        : INotificationHandler<StudentUpdatedEvent>
    {
        private readonly IStudentReadRepository _readRepository;

        public StudentUpdatedEventHandler(IStudentReadRepository readRepository)
            => _readRepository = readRepository;

        public async Task Handle(StudentUpdatedEvent notification, CancellationToken ct)
        {
            var existing = await _readRepository.GetByIdAsync(notification.StudentId.Value, ct);
            if (existing is null) return;

            existing.FirstName = notification.FirstName;
            existing.LastName  = notification.LastName;
            existing.FullName  = $"{notification.FirstName} {notification.LastName}";
            existing.Email     = notification.Email;
            existing.UpdatedAt = notification.UpdatedAt;

            await _readRepository.UpsertAsync(existing, ct);
        }
    }

    // ── Estudiante eliminado → borra de MongoDB directamente ─────────────────
    internal sealed class StudentDeletedEventHandler
        : INotificationHandler<StudentDeletedEvent>
    {
        private readonly IStudentReadRepository _readRepository;

        public StudentDeletedEventHandler(IStudentReadRepository readRepository)
            => _readRepository = readRepository;

        public Task Handle(StudentDeletedEvent notification, CancellationToken ct)
            => _readRepository.DeleteAsync(notification.StudentId.Value, ct);
    }

    // ── Asignatura creada → proyecta en MongoDB directamente ─────────────────
    internal sealed class SubjectCreatedEventHandler
        : INotificationHandler<SubjectCreatedEvent>
    {
        private readonly ISubjectReadRepository _readRepository;

        public SubjectCreatedEventHandler(ISubjectReadRepository readRepository)
            => _readRepository = readRepository;

        public Task Handle(SubjectCreatedEvent notification, CancellationToken ct)
        {
            var model = new SubjectReadModel
            {
                Id               = notification.SubjectId.Value,
                Name             = notification.Name,
                Code             = notification.Code,
                Description      = notification.Description,
                Credits          = notification.Credits,
                MaxStudents      = notification.MaxStudents,
                EnrolledStudents = 0,
                CreatedAt        = notification.CreatedAt
            };
            return _readRepository.UpsertAsync(model, ct);
        }
    }
}
