using System.ComponentModel.DataAnnotations;

namespace Students.Application.DTOs;

public record RegisterRequest(
    [Required, MinLength(3), MaxLength(50)]  string Username,
    [Required, EmailAddress, MaxLength(150)] string Email,
    [Required, MinLength(6), MaxLength(100)] string Password,
    string Role = "ReadOnly"   // Admin | Teacher | ReadOnly
);

public record LoginRequest(
    [Required] string Username,
    [Required] string Password
);

public record AuthResponse(
    string   Token,
    string   Username,
    string   Email,
    string   Role,
    DateTime ExpiresAt
);

public record ErrorResponse(
    string Message,
    int    StatusCode
);
