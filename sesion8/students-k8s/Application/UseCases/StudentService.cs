using Students.Application.DTOs;
using Students.Domain.Entities;
using Students.Domain.Interfaces;
using Students.Infrastructure.Messaging;

namespace Students.Application.UseCases;

public interface IStudentService
{
    Task<IEnumerable<StudentDto>> GetAllAsync(bool onlyActive = true);
    Task<StudentDto?>             GetByIdAsync(Guid id);
    Task<StudentDto>              CreateAsync(CreateStudentRequest request);
    Task<StudentDto?>             UpdateAsync(Guid id, UpdateStudentRequest request);
    Task<bool>                    DeleteAsync(Guid id);
}

public class StudentService : IStudentService
{
    private readonly IStudentRepository      _repo;
    private readonly IStudentEventPublisher  _events;
    private readonly ILogger<StudentService> _logger;

    public StudentService(
        IStudentRepository     repo,
        IStudentEventPublisher events,
        ILogger<StudentService> logger)
    {
        _repo   = repo;
        _events = events;
        _logger = logger;
    }

    public async Task<IEnumerable<StudentDto>> GetAllAsync(bool onlyActive = true)
        => (await _repo.GetAllAsync(onlyActive)).Select(ToDto);

    public async Task<StudentDto?> GetByIdAsync(Guid id)
    {
        var s = await _repo.GetByIdAsync(id);
        return s is null ? null : ToDto(s);
    }

    public async Task<StudentDto> CreateAsync(CreateStudentRequest req)
    {
        if (await _repo.GetByEmailAsync(req.Email) is not null)
            throw new InvalidOperationException($"Ya existe un alumno con el email '{req.Email}'.");

        if (await _repo.GetByEnrollmentAsync(req.EnrollmentNumber) is not null)
            throw new InvalidOperationException($"Ya existe un alumno con la matrícula '{req.EnrollmentNumber}'.");

        var student = Student.Create(
            req.FirstName, req.LastName, req.Email,
            req.EnrollmentNumber, req.DateOfBirth, req.Phone, req.Address);

        await _repo.AddAsync(student);

        // Publicar evento — enrollments-service puede reaccionar si es necesario
        await _events.PublishStudentCreatedAsync(
            student.Id, student.FirstName, student.LastName,
            student.Email, student.EnrollmentNumber);

        return ToDto(student);
    }

    public async Task<StudentDto?> UpdateAsync(Guid id, UpdateStudentRequest req)
    {
        var student = await _repo.GetByIdAsync(id);
        if (student is null) return null;

        var emailOwner = await _repo.GetByEmailAsync(req.Email);
        if (emailOwner is not null && emailOwner.Id != id)
            throw new InvalidOperationException($"El email '{req.Email}' ya está en uso.");

        student.Update(req.FirstName, req.LastName, req.Email,
                       req.DateOfBirth, req.Phone, req.Address);

        await _repo.UpdateAsync(student);
        return ToDto(student);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        // Obtener datos antes del soft-delete para incluirlos en el evento
        var student = await _repo.GetByIdAsync(id);
        if (student is null) return false;

        var success = await _repo.DeleteAsync(id);

        if (success)
        {
            _logger.LogInformation(
                "[EVENT] Alumno {Id} ({Name}) desactivado — publicando student.deactivated",
                id, student.FullName);

            // PATRÓN OBSERVER: publicar evento para que enrollments-service
            // cancele automáticamente todas las matrículas activas del alumno
            await _events.PublishStudentDeactivatedAsync(
                student.Id, student.FullName, student.Email);
        }

        return success;
    }

    private static StudentDto ToDto(Student s) => new(
        s.Id, s.FirstName, s.LastName, s.FullName,
        s.Email, s.EnrollmentNumber, s.DateOfBirth,
        s.Phone, s.Address, s.IsActive, s.CreatedAt, s.UpdatedAt);
}
