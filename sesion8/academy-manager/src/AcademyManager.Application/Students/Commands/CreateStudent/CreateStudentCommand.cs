using AcademyManager.Domain.Students;
using FluentValidation;
using MediatR;

namespace AcademyManager.Application.Students.Commands.CreateStudent
{
    // ─── Command ────────────────────────────────────────────────────────────────

    public sealed record CreateStudentCommand(
        string FirstName,
        string LastName,
        string Email,
        DateTime DateOfBirth) : IRequest<Guid>;

    // ─── Validator ───────────────────────────────────────────────────────────────

    public sealed class CreateStudentValidator : AbstractValidator<CreateStudentCommand>
    {
        public CreateStudentValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
            RuleFor(x => x.DateOfBirth)
                .LessThan(DateTime.UtcNow.AddYears(-16))
                .WithMessage("Student must be at least 16 years old.");
        }
    }

    // ─── Handler ─────────────────────────────────────────────────────────────────

    public sealed class CreateStudentHandler : IRequestHandler<CreateStudentCommand, Guid>
    {
        private readonly IStudentRepository _repository;

        public CreateStudentHandler(IStudentRepository repository) =>
            _repository = repository;

        public async Task<Guid> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
        {
            var existing = await _repository.GetByEmailAsync(request.Email, cancellationToken);
            if (existing is not null)
                throw new InvalidOperationException($"Email '{request.Email}' is already registered.");

            var student = Student.Create(
                request.FirstName,
                request.LastName,
                request.Email,
                request.DateOfBirth);

            await _repository.AddAsync(student, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            return student.Id.Value;
        }
    }
}
