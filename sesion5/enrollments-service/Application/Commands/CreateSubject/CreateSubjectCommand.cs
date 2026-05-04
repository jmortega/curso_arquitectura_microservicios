using Enrollments.Application.DTOs;
using Enrollments.Domain.Events;
using Enrollments.Domain.Exceptions;
using Enrollments.Domain.Factories;
using Enrollments.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Enrollments.Application.Commands.CreateSubject;

// ── Command ───────────────────────────────────────────────────────────────────
public record CreateSubjectCommand(
    string   Code,
    string   Name,
    string?  Description,
    int      Credits,
    int      MaxCapacity
) : IRequest<SubjectDto>;

// ── Validator ─────────────────────────────────────────────────────────────────
public sealed class CreateSubjectCommandValidator : AbstractValidator<CreateSubjectCommand>
{
    public CreateSubjectCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Credits).InclusiveBetween(1, 12);
        RuleFor(x => x.MaxCapacity).GreaterThan(0);
    }
}

// ── Handler — MEDIADOR (desacopla el Controller del caso de uso) ───────────────
public sealed class CreateSubjectCommandHandler : IRequestHandler<CreateSubjectCommand, SubjectDto>
{
    private readonly ISubjectRepository _repo;
    private readonly IEventPublisher    _events;

    public CreateSubjectCommandHandler(ISubjectRepository repo, IEventPublisher events)
    {
        _repo   = repo;
        _events = events;
    }

    public async Task<SubjectDto> Handle(CreateSubjectCommand cmd, CancellationToken ct)
    {
        if (await _repo.GetByCodeAsync(cmd.Code) is not null)
            throw new SubjectCodeAlreadyExistsException(cmd.Code);

        // FACTORY: crea la entidad validando las invariantes
        var subject = SubjectFactory.Create(cmd.Code, cmd.Name, cmd.Description, cmd.Credits, cmd.MaxCapacity);
        await _repo.AddAsync(subject);

        // OBSERVER: publica evento de dominio al bus de mensajes
        await _events.PublishAsync(new SubjectCreatedEvent(
            subject.Id, subject.Code, subject.Name, subject.Credits, subject.MaxCapacity));

        return ToDto(subject);
    }

    internal static SubjectDto ToDto(global::Enrollments.Domain.Entities.Subject s) => new(
        s.Id, s.Code, s.Name, s.Description, s.Credits,
        s.MaxCapacity, s.CurrentEnrollments, s.AvailableSlots,
        s.IsActive, s.CreatedAt, s.UpdatedAt);
}
