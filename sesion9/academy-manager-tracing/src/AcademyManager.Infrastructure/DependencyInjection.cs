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
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;

namespace AcademyManager.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ── Write DB: PostgreSQL via EF Core ──────────────────────────────
            services.AddDbContext<WriteDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("WriteDatabase"),
                    npgsql => npgsql.MigrationsAssembly(typeof(WriteDbContext).Assembly.FullName)));

            // Write-side repository adapters
            services.AddScoped<IStudentRepository, StudentRepository>();
            services.AddScoped<ISubjectRepository, SubjectRepository>();
            services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();

            // ── Read DB: MongoDB con instrumentación OpenTelemetry ────────────
            //
            //  MongoDB.Driver.Core.Extensions.DiagnosticSources intercepta todas
            //  las operaciones del driver (find, insert, update, delete) y las
            //  emite como Activity con el source name:
            //    "MongoDB.Driver.Core.Extensions.DiagnosticSources"
            //
            //  En Program.cs, AddSource() recoge esas actividades y las convierte
            //  en spans de OpenTelemetry con peer.service=mongodb-read, lo que
            //  permite a Jaeger dibujar la flecha academy-manager → mongodb-read
            //  en el grafo de dependencias.
            //
            services.AddSingleton<MongoDbContext>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var connectionString = config["MongoDB:ConnectionString"]
                    ?? throw new InvalidOperationException("MongoDB:ConnectionString is not configured.");

                var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);

                // Registrar el interceptor de OpenTelemetry en el driver de MongoDB
                mongoClientSettings.ClusterConfigurator = cb =>
                    cb.Subscribe(new DiagnosticsActivityEventSubscriber(new InstrumentationOptions
                    {
                        // CaptureCommandText incluye el JSON del filtro en el span
                        // (equivalente a SetDbStatementForText en EF Core)
                        // Deshabilitar en producción si los filtros contienen datos sensibles
                        CaptureCommandText = true
                    }));

                return new MongoDbContext(config, mongoClientSettings);
            });

            // Read-side repository adapters
            services.AddScoped<IStudentReadRepository, StudentReadRepository>();
            services.AddScoped<ISubjectReadRepository, SubjectReadRepository>();
            services.AddScoped<IEnrollmentReadRepository, EnrollmentReadRepository>();

            return services;
        }
    }
}
