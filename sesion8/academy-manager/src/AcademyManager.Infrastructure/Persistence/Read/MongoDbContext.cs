using AcademyManager.Application.Common.Interfaces;
using AcademyManager.Application.ReadModels;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;

namespace AcademyManager.Infrastructure.Persistence.Read
{
    /// <summary>
    /// MongoDB read-side context. Provides typed collection accessors.
    /// </summary>
    public sealed class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        // Registrar convenciones globales una sola vez (flag estático)
        private static bool _conventionsRegistered = false;
        private static readonly object _lock = new();

        public MongoDbContext(IConfiguration configuration)
        {
            RegisterConventions();

            var connectionString = configuration["MongoDB:ConnectionString"]
                ?? throw new InvalidOperationException("MongoDB:ConnectionString is not configured.");
            var databaseName = configuration["MongoDB:DatabaseName"] ?? "academy_read";

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        /// <summary>
        /// Registra:
        /// 1. CamelCaseElementNameConvention — mapea propiedades PascalCase (C#) a
        ///    campos camelCase en MongoDB (firstName, lastName, enrolledStudents...).
        /// 2. GuidSerializer(BsonType.String) — serializa Guid como string UUID
        ///    en lugar del subtipo binario 3/4 por defecto, compatible con los
        ///    _id almacenados como strings en el init script.
        /// </summary>
        private static void RegisterConventions()
        {
            lock (_lock)
            {
                if (_conventionsRegistered) return;

                // Guids serializados como strings ("b2000000-0000-...")
                BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));

                // Propiedades C# PascalCase → campos MongoDB camelCase
                var conventions = new ConventionPack
                {
                    new CamelCaseElementNameConvention(),
                    new IgnoreExtraElementsConvention(true)  // ignora campos extra en el doc
                };
                ConventionRegistry.Register("AcademyConventions", conventions, _ => true);

                _conventionsRegistered = true;
            }
        }

        public IMongoCollection<StudentReadModel> Students =>
            _database.GetCollection<StudentReadModel>("students");

        public IMongoCollection<SubjectReadModel> Subjects =>
            _database.GetCollection<SubjectReadModel>("subjects");

        public IMongoCollection<EnrollmentReadModel> Enrollments =>
            _database.GetCollection<EnrollmentReadModel>("enrollments");
    }

    namespace AcademyManager.Infrastructure.Persistence.Read.Repositories
    {
        internal sealed class StudentReadRepository : IStudentReadRepository
        {
            private readonly MongoDbContext _context;

            public StudentReadRepository(MongoDbContext context) => _context = context;

            public async Task<StudentReadModel?> GetByIdAsync(Guid id, CancellationToken ct) =>
                await _context.Students.Find(s => s.Id == id).FirstOrDefaultAsync(ct);

            public async Task<IEnumerable<StudentReadModel>> GetAllAsync(int page, int pageSize, CancellationToken ct) =>
                await _context.Students
                    .Find(FilterDefinition<StudentReadModel>.Empty)
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .SortBy(s => s.LastName)
                    .ToListAsync(ct);

            public async Task<long> CountAsync(CancellationToken ct) =>
                await _context.Students.CountDocumentsAsync(FilterDefinition<StudentReadModel>.Empty, cancellationToken: ct);

            public async Task UpsertAsync(StudentReadModel model, CancellationToken ct)
            {
                var filter = Builders<StudentReadModel>.Filter.Eq(s => s.Id, model.Id);
                await _context.Students.ReplaceOneAsync(filter, model, new ReplaceOptions { IsUpsert = true }, ct);
            }

            public async Task DeleteAsync(Guid id, CancellationToken ct) =>
                await _context.Students.DeleteOneAsync(s => s.Id == id, ct);

            public async Task AddEnrollmentSummaryAsync(Guid studentId, StudentReadModel.EnrollmentSummary summary, CancellationToken ct)
            {
                var filter = Builders<StudentReadModel>.Filter.Eq(s => s.Id, studentId);
                var update = Builders<StudentReadModel>.Update.Push(s => s.Enrollments, summary);
                await _context.Students.UpdateOneAsync(filter, update, cancellationToken: ct);
            }
        }

        internal sealed class SubjectReadRepository : ISubjectReadRepository
        {
            private readonly MongoDbContext _context;

            public SubjectReadRepository(MongoDbContext context) => _context = context;

            public async Task<SubjectReadModel?> GetByIdAsync(Guid id, CancellationToken ct) =>
                await _context.Subjects.Find(s => s.Id == id).FirstOrDefaultAsync(ct);

            public async Task<IEnumerable<SubjectReadModel>> GetAllAsync(CancellationToken ct) =>
                await _context.Subjects
                    .Find(FilterDefinition<SubjectReadModel>.Empty)
                    .SortBy(s => s.Name)
                    .ToListAsync(ct);

            public async Task UpsertAsync(SubjectReadModel model, CancellationToken ct)
            {
                var filter = Builders<SubjectReadModel>.Filter.Eq(s => s.Id, model.Id);
                await _context.Subjects.ReplaceOneAsync(filter, model, new ReplaceOptions { IsUpsert = true }, ct);
            }

            public async Task IncrementEnrolledStudentsAsync(Guid subjectId, int delta, CancellationToken ct)
            {
                var filter = Builders<SubjectReadModel>.Filter.Eq(s => s.Id, subjectId);
                var update = Builders<SubjectReadModel>.Update.Inc(s => s.EnrolledStudents, delta);
                await _context.Subjects.UpdateOneAsync(filter, update, cancellationToken: ct);
            }
        }

        internal sealed class EnrollmentReadRepository : IEnrollmentReadRepository
        {
            private readonly MongoDbContext _context;

            public EnrollmentReadRepository(MongoDbContext context) => _context = context;

            public async Task<IEnumerable<EnrollmentReadModel>> GetByStudentIdAsync(Guid studentId, CancellationToken ct) =>
                await _context.Enrollments.Find(e => e.StudentId == studentId).ToListAsync(ct);

            public async Task<IEnumerable<EnrollmentReadModel>> GetBySubjectIdAsync(Guid subjectId, CancellationToken ct) =>
                await _context.Enrollments.Find(e => e.SubjectId == subjectId).ToListAsync(ct);

            public async Task UpsertAsync(EnrollmentReadModel model, CancellationToken ct)
            {
                var filter = Builders<EnrollmentReadModel>.Filter.Eq(e => e.Id, model.Id);
                await _context.Enrollments.ReplaceOneAsync(filter, model, new ReplaceOptions { IsUpsert = true }, ct);
            }
        }
    }
}