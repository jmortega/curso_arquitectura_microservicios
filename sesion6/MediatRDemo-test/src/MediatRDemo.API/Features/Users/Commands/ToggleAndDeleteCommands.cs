using MediatR;
using MediatRDemo.API.Domain.Exceptions;
using MediatRDemo.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediatRDemo.API.Features.Users.Commands;

// ═══════════════════════════════════════════════════════════════════════════
// TOGGLE ACTIVE — activa o desactiva un usuario
// ═══════════════════════════════════════════════════════════════════════════

public record ToggleUserActiveCommand(Guid Id, bool Activate) : IRequest<bool>;

public sealed class ToggleUserActiveHandler
    : IRequestHandler<ToggleUserActiveCommand, bool>
{
    private readonly AppDbContext _db;

    public ToggleUserActiveHandler(AppDbContext db) => _db = db;

    public async Task<bool> Handle(
        ToggleUserActiveCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new UserNotFoundException(request.Id);

        if (request.Activate)
            user.Activate();
        else
            user.Deactivate();

        await _db.SaveChangesAsync(cancellationToken);
        return user.IsActive;
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// DELETE — elimina el usuario físicamente
// ═══════════════════════════════════════════════════════════════════════════

public record DeleteUserCommand(Guid Id) : IRequest<bool>;

public sealed class DeleteUserCommandHandler
    : IRequestHandler<DeleteUserCommand, bool>
{
    private readonly AppDbContext _db;

    public DeleteUserCommandHandler(AppDbContext db) => _db = db;

    public async Task<bool> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new UserNotFoundException(request.Id);

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
