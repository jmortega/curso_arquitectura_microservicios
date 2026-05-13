using AcademyManager.Application.Common.Interfaces;
using AcademyManager.Domain.Enrollments;
using AcademyManager.Domain.Students;
using AcademyManager.Domain.Subjects;
using AcademyManager.Infrastructure.Persistence.Read;
using AcademyManager.Infrastructure.Persistence.Read.AcademyManager.Infrastructure.Persistence.Read.Repositories;
using AcademyManager.Infrastructure.Persistence.Write;
using AcademyManager.Infrastructure.Persistence.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AcademyManager.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ── Write DB: PostgreSQL via EF Core ────────────────────────────────
            services.AddDbContext<WriteDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("WriteDatabase"),
                    npgsql => npgsql.MigrationsAssembly(typeof(WriteDbContext).Assembly.FullName)));

            // Write-side repository adapters
            services.AddScoped<IStudentRepository, StudentRepository>();
            services.AddScoped<ISubjectRepository, SubjectRepository>();
            services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();

            // ── Read DB: MongoDB ────────────────────────────────────────────────
            services.AddSingleton<MongoDbContext>();

            // Read-side repository adapters
            services.AddScoped<IStudentReadRepository, StudentReadRepository>();
            services.AddScoped<ISubjectReadRepository, SubjectReadRepository>();
            services.AddScoped<IEnrollmentReadRepository, EnrollmentReadRepository>();

            return services;
        }
    }
}
