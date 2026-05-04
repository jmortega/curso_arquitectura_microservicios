using Enrollments.Application.Commands.CreateSubject;
using Enrollments.Application.DTOs;
using Enrollments.Domain.Exceptions;
using Enrollments.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Enrollments.Application.Commands.UpdateSubject
{
    public record UpdateSubjectCommand(
        Guid    Id,
        string  Name,
        string? Description,
        int     Credits,
        int     MaxCapacity
    ) : IRequest<SubjectDto>;

    public sealed class UpdateSubjectValidator : AbstractValidator<UpdateSubjectCommand>
    {
        public UpdateSubjectValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Credits).InclusiveBetween(1, 12);
            RuleFor(x => x.MaxCapacity).GreaterThan(0);
        }
    }

    public sealed class UpdateSubjectCommandHandler : IRequestHandler<UpdateSubjectCommand, SubjectDto>
    {
        private readonly ISubjectRepository _repo;
        public UpdateSubjectCommandHandler(ISubjectRepository repo) => _repo = repo;

        public async Task<SubjectDto> Handle(UpdateSubjectCommand cmd, CancellationToken ct)
        {
            var subject = await _repo.GetByIdAsync(cmd.Id)
                ?? throw new SubjectNotFoundException(cmd.Id);

            subject.Update(cmd.Name, cmd.Description, cmd.Credits, cmd.MaxCapacity);
            await _repo.UpdateAsync(subject);
            return CreateSubjectCommandHandler.ToDto(subject);
        }
    }
}

namespace Enrollments.Application.Commands.DeleteSubject
{
    public record DeleteSubjectCommand(Guid Id) : IRequest<bool>;

    public sealed class DeleteSubjectCommandHandler : IRequestHandler<DeleteSubjectCommand, bool>
    {
        private readonly ISubjectRepository _repo;
        public DeleteSubjectCommandHandler(ISubjectRepository repo) => _repo = repo;

        public async Task<bool> Handle(DeleteSubjectCommand cmd, CancellationToken ct)
        {
            _ = await _repo.GetByIdAsync(cmd.Id)
                ?? throw new SubjectNotFoundException(cmd.Id);
            return await _repo.DeleteAsync(cmd.Id);
        }
    }
}
