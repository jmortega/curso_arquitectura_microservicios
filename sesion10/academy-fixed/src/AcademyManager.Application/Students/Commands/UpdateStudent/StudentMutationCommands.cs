using AcademyManager.Domain.Students;
using FluentValidation;
using MediatR;

namespace AcademyManager.Application.Students.Commands.UpdateStudent
{
    // ─── Update Command ──────────────────────────────────────────────────────────

    public sealed record UpdateStudentCommand(
        Guid StudentId,
        string FirstName,
        string LastName,
        string Email) : IRequest;

    public sealed class UpdateStudentValidator : AbstractValidator<UpdateStudentCommand>
    {
        public UpdateStudentValidator()
        {
            RuleFor(x => x.StudentId).NotEmpty();
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        }
    }

    public sealed class UpdateStudentHandler : IRequestHandler<UpdateStudentCommand>
    {
        private readonly IStudentRepository _repository;

        public UpdateStudentHandler(IStudentRepository repository) =>
            _repository = repository;

        public async Task Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
        {
            var student = await _repository.GetByIdAsync(
                StudentId.From(request.StudentId), cancellationToken)
                ?? throw new KeyNotFoundException($"Student {request.StudentId} not found.");

            student.Update(request.FirstName, request.LastName, request.Email);
            _repository.Update(student);
            await _repository.SaveChangesAsync(cancellationToken);
        }
    }

    // ─── Delete Command ──────────────────────────────────────────────────────────

    namespace AcademyManager.Application.Students.Commands.DeleteStudent
    {
        public sealed record DeleteStudentCommand(Guid StudentId) : IRequest;

        public sealed class DeleteStudentHandler : IRequestHandler<DeleteStudentCommand>
        {
            private readonly IStudentRepository _repository;

            public DeleteStudentHandler(IStudentRepository repository) =>
                _repository = repository;

            public async Task Handle(DeleteStudentCommand request, CancellationToken cancellationToken)
            {
                var student = await _repository.GetByIdAsync(
                    StudentId.From(request.StudentId), cancellationToken)
                    ?? throw new KeyNotFoundException($"Student {request.StudentId} not found.");

                student.MarkDeleted();
                _repository.Remove(student);
                await _repository.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
