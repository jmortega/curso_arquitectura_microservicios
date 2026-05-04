using MediatR;
using MediatRDemo.API.Features.Common;
using MediatRDemo.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediatRDemo.API.Features.Users.Queries;

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Consulta para obtener todos los usuarios con filtros opcionales.
/// Un Query en MediatR implementa IRequest&lt;TResponse&gt;.
/// </summary>
public record GetAllUsersQuery(
    bool? OnlyActive = null,
    string? Role     = null
) : IRequest<IEnumerable<UserDto>>;

// ── Handler ───────────────────────────────────────────────────────────────────

/// <summary>
/// Handler que procesa GetAllUsersQuery.
/// Implementa IRequestHandler&lt;TRequest, TResponse&gt;.
/// El mediador lo resuelve automáticamente por inyección de dependencias.
/// </summary>
public sealed class GetAllUsersQueryHandler
    : IRequestHandler<GetAllUsersQuery, IEnumerable<UserDto>>
{
    private readonly AppDbContext _db;

    public GetAllUsersQueryHandler(AppDbContext db) => _db = db;

    public async Task<IEnumerable<UserDto>> Handle(
        GetAllUsersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Users.AsNoTracking();

        if (request.OnlyActive.HasValue)
            query = query.Where(u => u.IsActive == request.OnlyActive.Value);

        if (!string.IsNullOrWhiteSpace(request.Role))
            query = query.Where(u => u.Role == request.Role);

        return await query
            .OrderBy(u => u.Name)
            .Select(u => new UserDto(
                u.Id, u.Name, u.Email,
                u.Role, u.IsActive, u.CreatedAt, u.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
