using AcademyManager.Application.Common.Interfaces;
using AcademyManager.Application.ReadModels;
using AcademyManager.Domain.Subjects;
using FluentValidation;
using MediatR;

namespace AcademyManager.Application.Subjects.Commands.CreateSubject
{
    // ─── Create Subject Command ──────────────────────────────────────────────────

    public sealed record CreateSubjectCommand(
        string Name,
        string Code,
        string Description,
        int Credits,
        int MaxStudents) : IRequest<Guid>;

    public sealed class CreateSubjectValidator : AbstractValidator<CreateSubjectCommand>
    {
        public CreateSubjectValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.Credits).InclusiveBetween(1, 12);
            RuleFor(x => x.MaxStudents).InclusiveBetween(1, 500);
        }
    }

    public sealed class CreateSubjectHandler : IRequestHandler<CreateSubjectCommand, Guid>
    {
        private readonly ISubjectRepository _repository;

        public CreateSubjectHandler(ISubjectRepository repository) =>
            _repository = repository;

        public async Task<Guid> Handle(CreateSubjectCommand request, CancellationToken ct)
        {
            var existing = await _repository.GetByCodeAsync(request.Code, ct);
            if (existing is not null)
                throw new InvalidOperationException($"Subject code '{request.Code}' already exists.");

            var subject = Subject.Create(
                request.Name, request.Code, request.Description,
                request.Credits, request.MaxStudents);

            await _repository.AddAsync(subject, ct);
            await _repository.SaveChangesAsync(ct);

            return subject.Id.Value;
        }
    }

    // ─── Get All Subjects Query ───────────────────────────────────────────────────

    namespace AcademyManager.Application.Subjects.Queries.GetAllSubjects
    {
        public sealed record GetAllSubjectsQuery : IRequest<IEnumerable<SubjectReadModel>>;

        public sealed class GetAllSubjectsHandler : IRequestHandler<GetAllSubjectsQuery, IEnumerable<SubjectReadModel>>
        {
            private readonly ISubjectReadRepository _readRepository;

            public GetAllSubjectsHandler(ISubjectReadRepository readRepository) =>
                _readRepository = readRepository;

            public Task<IEnumerable<SubjectReadModel>> Handle(GetAllSubjectsQuery request, CancellationToken ct) =>
                _readRepository.GetAllAsync(ct);
        }
    }
}
