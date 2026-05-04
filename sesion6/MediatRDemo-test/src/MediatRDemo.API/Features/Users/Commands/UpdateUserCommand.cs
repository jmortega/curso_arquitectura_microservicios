using FluentValidation;
using MediatR;
using MediatRDemo.API.Domain.Exceptions;
using MediatRDemo.API.Features.Common;
using MediatRDemo.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediatRDemo.API.Features.Users.Commands;

// ── Command ───────────────────────────────────────────────────────────────────

public record UpdateUserCommand(
    Guid   Id,
    string Name,
    string Email,
    string Role
) : IRequest<UserDto>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    private static readonly string[] ValidRoles = ["Admin", "User", "ReadOnly"];

    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Role)
            .Must(r => ValidRoles.Contains(r))
            .WithMessage($"Rol inválido. Valores permitidos: {string.Join(", ", ValidRoles)}.");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class UpdateUserCommandHandler
    : IRequestHandler<UpdateUserCommand, UserDto>
{
    private readonly AppDbContext _db;

    public UpdateUserCommandHandler(AppDbContext db) => _db = db;

    public async Task<UserDto> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new UserNotFoundException(request.Id);

        // Verificar que el nuevo email no pertenezca a otro usuario
        var emailTaken = await _db.Users
            .AnyAsync(u => u.Email == request.Email.ToLowerInvariant()
                        && u.Id    != request.Id, cancellationToken);

        if (emailTaken)
            throw new EmailAlreadyExistsException(request.Email);

        // Delegar la lógica de negocio a la entidad
        user.Update(request.Name, request.Email, request.Role);

        await _db.SaveChangesAsync(cancellationToken);

        return new UserDto(
            user.Id, user.Name, user.Email,
            user.Role, user.IsActive, user.CreatedAt, user.UpdatedAt);
    }
}
