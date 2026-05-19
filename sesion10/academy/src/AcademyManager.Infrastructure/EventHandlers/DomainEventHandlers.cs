using AcademyManager.Application.Common.Interfaces;
using AcademyManager.Application.ReadModels;
using AcademyManager.Domain.Enrollments.Events;
using AcademyManager.Domain.Students;
using AcademyManager.Domain.Students.Events;
using AcademyManager.Domain.Subjects;
using AcademyManager.Domain.Subjects.Events;
using MediatR;

namespace AcademyManager.Infrastructure.EventHandlers
{
    /// <summary>
    /// Listens to StudentCreatedEvent and projects it into the MongoDB read model.
    /// This is the "write to read DB" side of CQRS.
    /// </summary>
    internal sealed class StudentCreatedEventHandler : INotificationHandler<StudentCreatedEvent>
    {
        private readonly IStudentReadRepository _readRepository;

        public StudentCreatedEventHandler(IStudentReadRepository readRepository) =>
            _readRepository = readRepository;

        public Task Handle(StudentCreatedEvent notification, CancellationToken ct)
        {
            var model = new StudentReadModel
            {
                Id = notification.StudentId.Value,
                FirstName = notification.FirstName,
                LastName = notification.LastName,
                FullName = $"{notification.FirstName} {notification.LastName}",
                Email = notification.Email,
                DateOfBirth = notification.DateOfBirth,
                CreatedAt = notification.CreatedAt,
                Enrollments = new List<StudentReadModel.EnrollmentSummary>()
            };
            return _readRepository.UpsertAsync(model, ct);
        }
    }

    internal sealed class StudentUpdatedEventHandler : INotificationHandler<StudentUpdatedEvent>
    {
        private readonly IStudentReadRepository _readRepository;

        public StudentUpdatedEventHandler(IStudentReadRepository readRepository) =>
            _readRepository = readRepository;

        public async Task Handle(StudentUpdatedEvent notification, CancellationToken ct)
        {
            var existing = await _readRepository.GetByIdAsync(notification.StudentId.Value, ct);
            if (existing is null) return;

            existing.FirstName = notification.FirstName;
            existing.LastName = notification.LastName;
            existing.FullName = $"{notification.FirstName} {notification.LastName}";
            existing.Email = notification.Email;
            existing.UpdatedAt = notification.UpdatedAt;

            await _readRepository.UpsertAsync(existing, ct);
        }
    }

    internal sealed class StudentDeletedEventHandler : INotificationHandler<StudentDeletedEvent>
    {
        private readonly IStudentReadRepository _readRepository;

        public StudentDeletedEventHandler(IStudentReadRepository readRepository) =>
            _readRepository = readRepository;

        public Task Handle(StudentDeletedEvent notification, CancellationToken ct) =>
            _readRepository.DeleteAsync(notification.StudentId.Value, ct);
    }

    internal sealed class SubjectCreatedEventHandler : INotificationHandler<SubjectCreatedEvent>
    {
        private readonly ISubjectReadRepository _readRepository;

        public SubjectCreatedEventHandler(ISubjectReadRepository readRepository) =>
            _readRepository = readRepository;

        public Task Handle(SubjectCreatedEvent notification, CancellationToken ct)
        {
            var model = new SubjectReadModel
            {
                Id = notification.SubjectId.Value,
                Name = notification.Name,
                Code = notification.Code,
                Description = notification.Description,
                Credits = notification.Credits,
                MaxStudents = notification.MaxStudents,
                EnrolledStudents = 0,
                CreatedAt = notification.CreatedAt
            };
            return _readRepository.UpsertAsync(model, ct);
        }
    }

    internal sealed class EnrollmentCreatedEventHandler : INotificationHandler<EnrollmentCreatedEvent>
    {
        private readonly IEnrollmentReadRepository _enrollmentReadRepo;
        private readonly IStudentReadRepository _studentReadRepo;
        private readonly ISubjectReadRepository _subjectReadRepo;
        private readonly IStudentRepository _studentWriteRepo;
        private readonly ISubjectRepository _subjectWriteRepo;

        public EnrollmentCreatedEventHandler(
            IEnrollmentReadRepository enrollmentReadRepo,
            IStudentReadRepository studentReadRepo,
            ISubjectReadRepository subjectReadRepo,
            IStudentRepository studentWriteRepo,
            ISubjectRepository subjectWriteRepo)
        {
            _enrollmentReadRepo = enrollmentReadRepo;
            _studentReadRepo = studentReadRepo;
            _subjectReadRepo = subjectReadRepo;
            _studentWriteRepo = studentWriteRepo;
            _subjectWriteRepo = subjectWriteRepo;
        }

        public async Task Handle(EnrollmentCreatedEvent notification, CancellationToken ct)
        {
            // Fetch names from write DB (authoritative source)
            var student = await _studentWriteRepo.GetByIdAsync(notification.StudentId, ct);
            var subject = await _subjectWriteRepo.GetByIdAsync(notification.SubjectId, ct);

            if (student is null || subject is null) return;

            // 1. Upsert denormalized enrollment read model
            var enrollmentModel = new EnrollmentReadModel
            {
                Id = notification.EnrollmentId.Value,
                StudentId = notification.StudentId.Value,
                StudentName = student.Name.FullName,
                StudentEmail = student.Email.Value,
                SubjectId = notification.SubjectId.Value,
                SubjectName = subject.Name,
                SubjectCode = subject.Code,
                Status = "Active",
                EnrolledAt = notification.EnrolledAt
            };
            await _enrollmentReadRepo.UpsertAsync(enrollmentModel, ct);

            // 2. Add enrollment summary to student read model
            var summary = new StudentReadModel.EnrollmentSummary
            {
                EnrollmentId = notification.EnrollmentId.Value,
                SubjectId = notification.SubjectId.Value,
                SubjectName = subject.Name,
                SubjectCode = subject.Code,
                Status = "Active",
                EnrolledAt = notification.EnrolledAt
            };
            await _studentReadRepo.AddEnrollmentSummaryAsync(notification.StudentId.Value, summary, ct);

            // 3. Increment enrolled students counter in subject read model
            await _subjectReadRepo.IncrementEnrolledStudentsAsync(notification.SubjectId.Value, 1, ct);
        }
    }
}
