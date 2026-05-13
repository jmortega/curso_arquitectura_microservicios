using AcademyManager.Application.Common.Interfaces;
using AcademyManager.Application.ReadModels;
using AcademyManager.Domain.Enrollments;
using AcademyManager.Domain.Students;
using AcademyManager.Domain.Subjects;
using FluentValidation;
using MediatR;

namespace AcademyManager.Application.Enrollments.Commands.EnrollStudent
{
    // ─── Enroll Student Command ──────────────────────────────────────────────────

    public sealed record EnrollStudentCommand(Guid StudentId, Guid SubjectId) : IRequest<Guid>;

    public sealed class EnrollStudentValidator : AbstractValidator<EnrollStudentCommand>
    {
        public EnrollStudentValidator()
        {
            RuleFor(x => x.StudentId).NotEmpty();
            RuleFor(x => x.SubjectId).NotEmpty();
        }
    }

    public sealed class EnrollStudentHandler : IRequestHandler<EnrollStudentCommand, Guid>
    {
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ISubjectRepository _subjectRepository;

        public EnrollStudentHandler(
            IEnrollmentRepository enrollmentRepository,
            IStudentRepository studentRepository,
            ISubjectRepository subjectRepository)
        {
            _enrollmentRepository = enrollmentRepository;
            _studentRepository = studentRepository;
            _subjectRepository = subjectRepository;
        }

        public async Task<Guid> Handle(EnrollStudentCommand request, CancellationToken ct)
        {
            var studentId = StudentId.From(request.StudentId);
            var subjectId = SubjectId.From(request.SubjectId);

            if (!await _studentRepository.ExistsAsync(studentId, ct))
                throw new KeyNotFoundException($"Student {request.StudentId} not found.");

            if (!await _subjectRepository.ExistsAsync(subjectId, ct))
                throw new KeyNotFoundException($"Subject {request.SubjectId} not found.");

            if (await _enrollmentRepository.ExistsAsync(studentId, subjectId, ct))
                throw new InvalidOperationException("Student is already enrolled in this subject.");

            var subject = await _subjectRepository.GetByIdAsync(subjectId, ct)!;
            var activeCount = await _enrollmentRepository.CountActiveBySubjectAsync(subjectId, ct);

            if (activeCount >= subject!.MaxStudents)
                throw new InvalidOperationException($"Subject '{subject.Name}' has reached maximum capacity.");

            var enrollment = Enrollment.Create(studentId, subjectId);
            await _enrollmentRepository.AddAsync(enrollment, ct);
            await _enrollmentRepository.SaveChangesAsync(ct);

            return enrollment.Id.Value;
        }
    }

    // ─── Cancel Enrollment Command ────────────────────────────────────────────────

    namespace AcademyManager.Application.Enrollments.Commands.CancelEnrollment
    {
        public sealed record CancelEnrollmentCommand(Guid EnrollmentId) : IRequest;

        public sealed class CancelEnrollmentHandler : IRequestHandler<CancelEnrollmentCommand>
        {
            private readonly IEnrollmentRepository _repository;

            public CancelEnrollmentHandler(IEnrollmentRepository repository) =>
                _repository = repository;

            public async Task Handle(CancelEnrollmentCommand request, CancellationToken ct)
            {
                var enrollment = await _repository.GetByIdAsync(EnrollmentId.From(request.EnrollmentId), ct)
                    ?? throw new KeyNotFoundException($"Enrollment {request.EnrollmentId} not found.");

                enrollment.Cancel();
                _repository.Update(enrollment);
                await _repository.SaveChangesAsync(ct);
            }
        }

        // ─── Get Student Enrollments Query ───────────────────────────────────────────

        namespace AcademyManager.Application.Enrollments.Queries.GetStudentEnrollments
        {
            public sealed record GetStudentEnrollmentsQuery(Guid StudentId) : IRequest<IEnumerable<EnrollmentReadModel>>;

            public sealed class GetStudentEnrollmentsHandler
                : IRequestHandler<GetStudentEnrollmentsQuery, IEnumerable<EnrollmentReadModel>>
            {
                private readonly IEnrollmentReadRepository _readRepository;

                public GetStudentEnrollmentsHandler(IEnrollmentReadRepository readRepository) =>
                    _readRepository = readRepository;

                public Task<IEnumerable<EnrollmentReadModel>> Handle(
                    GetStudentEnrollmentsQuery request, CancellationToken ct) =>
                    _readRepository.GetByStudentIdAsync(request.StudentId, ct);
            }
        }
    }
}
