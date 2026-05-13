using Students.Domain.Entities;

namespace Students.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<bool>  ExistsAsync(string username, string email);
    Task<Guid>  CreateAsync(User user);
}
