using FluentValidation;
using MediatR;
using MediatRDemo.API.Domain.Entities;
using MediatRDemo.API.Domain.Exceptions;
using MediatRDemo.API.Features.Common;
using MediatRDemo.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediatRDemo.API.Features.Users.Commands;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>
/// Comando para crear un usuario.
/// Un Command en MediatR implementa IRequest&lt;TResponse&gt; pero representa
/// una MUTACIÓN del estado del sistema (vs. una Query que solo lee).
/// </summary>
public record CreateUserCommand(
    string Name,
    string Email,
    string Role = "User"
) : IRequest<UserDto>;

// ── Validator (FluentValidation) ──────────────────────────────────────────────

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    private static readonly string[] ValidRoles = ["Admin", "User", "ReadOnly"];

    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar 100 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El formato del email no es válido.")
            .MaximumLength(150);

        RuleFor(x => x.Role)
            .Must(r => ValidRoles.Contains(r))
            .WithMessage($"El rol debe ser uno de: {string.Join(", ", ValidRoles)}.");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly AppDbContext _db;

    public CreateUserCommandHandler(AppDbContext db) => _db = db;

    public async Task<UserDto> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        // Verificar email único
        var exists = await _db.Users
            .AnyAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (exists)
            throw new EmailAlreadyExistsException(request.Email);

        // Crear la entidad de dominio
        var user = User.Create(request.Name, request.Email, request.Role);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return new UserDto(
            user.Id, user.Name, user.Email,
            user.Role, user.IsActive, user.CreatedAt, user.UpdatedAt);
    }
}
