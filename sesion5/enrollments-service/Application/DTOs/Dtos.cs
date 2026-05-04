using System.ComponentModel.DataAnnotations;

namespace Enrollments.Application.DTOs;

// ── Subject DTOs ──────────────────────────────────────────────────────────────

public record SubjectDto(
    Guid     Id,
    string   Code,
    string   Name,
    string?  Description,
    int      Credits,
    int      MaxCapacity,
    int      CurrentEnrollments,
    int      AvailableSlots,
    bool     IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateSubjectRequest(
    [Required, MaxLength(20)]  string   Code,
    [Required, MaxLength(150)] string   Name,
    [MaxLength(500)]           string?  Description,
    [Range(1, 12)]             int      Credits,
    [Range(1, 500)]            int      MaxCapacity
);

public record UpdateSubjectRequest(
    [Required, MaxLength(150)] string   Name,
    [MaxLength(500)]           string?  Description,
    [Range(1, 12)]             int      Credits,
    [Range(1, 500)]            int      MaxCapacity
);

// ── Enrollment DTOs ───────────────────────────────────────────────────────────

public record EnrollmentDto(
    Guid      Id,
    Guid      StudentId,
    string?   StudentName,
    Guid      SubjectId,
    string?   SubjectName,
    string?   SubjectCode,
    string    Status,
    string?   Notes,
    DateTime  EnrolledAt,
    DateTime? CancelledAt,
    DateTime  UpdatedAt
);

public record EnrollStudentRequest(
    [Required] Guid    StudentId,
    [Required] Guid    SubjectId,
    [MaxLength(300)]   string? Notes
);

public record CancelEnrollmentRequest(
    [MaxLength(300)] string? Reason
);
