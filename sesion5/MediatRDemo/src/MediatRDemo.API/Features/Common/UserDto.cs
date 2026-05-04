namespace MediatRDemo.API.Features.Common;

/// <summary>DTO compartido para respuestas de usuario.</summary>
public record UserDto(
    Guid     Id,
    string   Name,
    string   Email,
    string   Role,
    bool     IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
