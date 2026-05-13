using Students.Domain.Entities;

namespace Students.Domain.Interfaces;

public interface IStudentRepository
{
    Task<IEnumerable<Student>> GetAllAsync(bool onlyActive = true);
    Task<Student?>             GetByIdAsync(Guid id);
    Task<Student?>             GetByEmailAsync(string email);
    Task<Student?>             GetByEnrollmentAsync(string enrollmentNumber);
    Task<Guid>                 AddAsync(Student student);
    Task<bool>                 UpdateAsync(Student student);
    Task<bool>                 DeleteAsync(Guid id);
}