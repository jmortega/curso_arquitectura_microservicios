using MediatR;
using MediatRDemo.API.Domain.Exceptions;
using MediatRDemo.API.Features.Common;
using MediatRDemo.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediatRDemo.API.Features.Users.Queries;

// ── Query ─────────────────────────────────────────────────────────────────────

public record GetUserByIdQuery(Guid Id) : IRequest<UserDto>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetUserByIdQueryHandler
    : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly AppDbContext _db;

    public GetUserByIdQueryHandler(AppDbContext db) => _db = db;

    public async Task<UserDto> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new UserNotFoundException(request.Id);

        return new UserDto(
            user.Id, user.Name, user.Email,
            user.Role, user.IsActive, user.CreatedAt, user.UpdatedAt);
    }
}
