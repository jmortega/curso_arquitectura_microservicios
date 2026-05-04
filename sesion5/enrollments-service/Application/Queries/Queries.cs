using Enrollments.Application.Commands.CreateSubject;
using Enrollments.Application.Commands.EnrollStudent;
using Enrollments.Application.DTOs;
using Enrollments.Domain.Exceptions;
using Enrollments.Domain.Interfaces;
using MediatR;

namespace Enrollments.Application.Queries.GetAllSubjects
{
    public record GetAllSubjectsQuery(bool OnlyActive = true) : IRequest<IEnumerable<SubjectDto>>;

    public sealed class GetAllSubjectsQueryHandler : IRequestHandler<GetAllSubjectsQuery, IEnumerable<SubjectDto>>
    {
        private readonly ISubjectRepository _repo;
        public GetAllSubjectsQueryHandler(ISubjectRepository repo) => _repo = repo;

        public async Task<IEnumerable<SubjectDto>> Handle(GetAllSubjectsQuery q, CancellationToken ct)
            => (await _repo.GetAllAsync(q.OnlyActive)).Select(CreateSubjectCommandHandler.ToDto);
    }
}

namespace Enrollments.Application.Queries.GetSubjectById
{
    public record GetSubjectByIdQuery(Guid Id) : IRequest<SubjectDto>;

    public sealed class GetSubjectByIdQueryHandler : IRequestHandler<GetSubjectByIdQuery, SubjectDto>
    {
        private readonly ISubjectRepository _repo;
        public GetSubjectByIdQueryHandler(ISubjectRepository repo) => _repo = repo;

        public async Task<SubjectDto> Handle(GetSubjectByIdQuery q, CancellationToken ct)
        {
            var subject = await _repo.GetByIdAsync(q.Id)
                ?? throw new SubjectNotFoundException(q.Id);
            return CreateSubjectCommandHandler.ToDto(subject);
        }
    }
}

namespace Enrollments.Application.Queries.GetAllEnrollments
{
    public record GetAllEnrollmentsQuery : IRequest<IEnumerable<EnrollmentDto>>;

    public sealed class GetAllEnrollmentsQueryHandler : IRequestHandler<GetAllEnrollmentsQuery, IEnumerable<EnrollmentDto>>
    {
        private readonly IEnrollmentRepository _repo;
        public GetAllEnrollmentsQueryHandler(IEnrollmentRepository repo) => _repo = repo;

        public async Task<IEnumerable<EnrollmentDto>> Handle(GetAllEnrollmentsQuery q, CancellationToken ct)
            => (await _repo.GetAllAsync()).Select(EnrollStudentCommandHandler.ToDto);
    }
}

namespace Enrollments.Application.Queries.GetEnrollmentsByStudent
{
    public record GetEnrollmentsByStudentQuery(Guid StudentId) : IRequest<IEnumerable<EnrollmentDto>>;

    public sealed class GetEnrollmentsByStudentQueryHandler
        : IRequestHandler<GetEnrollmentsByStudentQuery, IEnumerable<EnrollmentDto>>
    {
        private readonly IEnrollmentRepository _repo;
        public GetEnrollmentsByStudentQueryHandler(IEnrollmentRepository repo) => _repo = repo;

        public async Task<IEnumerable<EnrollmentDto>> Handle(GetEnrollmentsByStudentQuery q, CancellationToken ct)
            => (await _repo.GetByStudentIdAsync(q.StudentId)).Select(EnrollStudentCommandHandler.ToDto);
    }
}

namespace Enrollments.Application.Queries.GetEnrollmentsBySubject
{
    public record GetEnrollmentsBySubjectQuery(Guid SubjectId) : IRequest<IEnumerable<EnrollmentDto>>;

    public sealed class GetEnrollmentsBySubjectQueryHandler
        : IRequestHandler<GetEnrollmentsBySubjectQuery, IEnumerable<EnrollmentDto>>
    {
        private readonly IEnrollmentRepository _repo;
        public GetEnrollmentsBySubjectQueryHandler(IEnrollmentRepository repo) => _repo = repo;

        public async Task<IEnumerable<EnrollmentDto>> Handle(GetEnrollmentsBySubjectQuery q, CancellationToken ct)
            => (await _repo.GetBySubjectIdAsync(q.SubjectId)).Select(EnrollStudentCommandHandler.ToDto);
    }
}
