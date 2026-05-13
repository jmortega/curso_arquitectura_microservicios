using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AcademyManager.Application.ReadModels
{
    public class StudentReadModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public DateTime DateOfBirth { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<EnrollmentSummary> Enrollments { get; set; } = new();

        public class EnrollmentSummary
        {
            public Guid EnrollmentId { get; set; }
            public Guid SubjectId { get; set; }
            public string SubjectName { get; set; } = default!;
            public string SubjectCode { get; set; } = default!;
            public string Status { get; set; } = default!;
            public DateTime EnrolledAt { get; set; }
        }
    }

    public class SubjectReadModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
        public int Credits { get; set; }
        public int MaxStudents { get; set; }
        public int EnrolledStudents { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EnrollmentReadModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = default!;
        public string StudentEmail { get; set; } = default!;
        public Guid SubjectId { get; set; }
        public string SubjectName { get; set; } = default!;
        public string SubjectCode { get; set; } = default!;
        public string Status { get; set; } = default!;
        public DateTime EnrolledAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
