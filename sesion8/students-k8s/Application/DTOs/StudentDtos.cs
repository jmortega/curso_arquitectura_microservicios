using System.ComponentModel.DataAnnotations;

namespace Students.Application.DTOs;

public record StudentDto(
    Guid      Id,
    string    FirstName,
    string    LastName,
    string    FullName,
    string    Email,
    string    EnrollmentNumber,
    DateOnly? DateOfBirth,
    string?   Phone,
    string?   Address,
    bool      IsActive,
    DateTime  CreatedAt,
    DateTime  UpdatedAt
);

public record CreateStudentRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, EmailAddress, MaxLength(150)] string Email,
    [Required, MaxLength(20)] string EnrollmentNumber,
    DateOnly? DateOfBirth,
    [MaxLength(20)]  string? Phone,
    [MaxLength(255)] string? Address
);

public record UpdateStudentRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, EmailAddress, MaxLength(150)] string Email,
    DateOnly? DateOfBirth,
    [MaxLength(20)]  string? Phone,
    [MaxLength(255)] string? Address
);