using Students.Application.UseCases;
using Students.Domain.Interfaces;
using Students.Infrastructure.Persistence.Repositories;

namespace Students.Infrastructure.Configuration;

public static class ServiceExtensions
{
    public static IServiceCollection AddStudentsServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddScoped<IStudentRepository>(_ => new StudentRepository(connectionString));
        services.AddScoped<IStudentService, StudentService>();

        return services;
    }
}