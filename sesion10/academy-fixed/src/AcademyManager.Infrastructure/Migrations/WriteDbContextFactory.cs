using AcademyManager.Infrastructure.Persistence.Write;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AcademyManager.Infrastructure.Migrations
{
    /// <summary>
    /// Design-time factory: allows 'dotnet ef migrations add' to find the DbContext
    /// without needing the full application host running.
    /// </summary>
    public sealed class WriteDbContextFactory : IDesignTimeDbContextFactory<WriteDbContext>
    {
        public WriteDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = config.GetConnectionString("WriteDatabase")
                ?? "Host=localhost;Port=5432;Database=academy_write;Username=academy;Password=academy_pass";

            var optionsBuilder = new DbContextOptionsBuilder<WriteDbContext>();
            optionsBuilder.UseNpgsql(connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(WriteDbContext).Assembly.FullName));

            // MediatR stub for design-time (no events dispatched)
            var mediator = new DesignTimeMediatorStub();
            return new WriteDbContext(optionsBuilder.Options, mediator);
        }
    }

    /// <summary>Stub IMediator for design-time tools (no-op publish).</summary>
    internal sealed class DesignTimeMediatorStub : MediatR.IMediator
    {
        public Task<TResponse> Send<TResponse>(MediatR.IRequest<TResponse> request, CancellationToken ct = default)
            => Task.FromResult(default(TResponse)!);
        public Task Send<TRequest>(TRequest request, CancellationToken ct = default)
            where TRequest : MediatR.IRequest => Task.CompletedTask;
        public Task<object?> Send(object request, CancellationToken ct = default)
            => Task.FromResult<object?>(null);
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(MediatR.IStreamRequest<TResponse> request, CancellationToken ct = default)
            => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task Publish(object notification, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
            where TNotification : MediatR.INotification => Task.CompletedTask;
    }
}
