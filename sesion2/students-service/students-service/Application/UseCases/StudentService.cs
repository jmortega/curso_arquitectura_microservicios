using Students.Application.DTOs;
using Students.Domain.Entities;
using Students.Domain.Interfaces;

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
    private readonly IStudentRepository _repo;

    public StudentService(IStudentRepository repo) => _repo = repo;

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

    public Task<bool> DeleteAsync(Guid id) => _repo.DeleteAsync(id);

    private static StudentDto ToDto(Student s) => new(
        s.Id, s.FirstName, s.LastName, s.FullName,
        s.Email, s.EnrollmentNumber, s.DateOfBirth,
        s.Phone, s.Address, s.IsActive, s.CreatedAt, s.UpdatedAt);
}