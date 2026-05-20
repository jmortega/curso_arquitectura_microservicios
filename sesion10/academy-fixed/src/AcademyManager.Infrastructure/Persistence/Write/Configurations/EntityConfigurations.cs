using AcademyManager.Domain.Enrollments;
using AcademyManager.Domain.Students;
using AcademyManager.Domain.Subjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademyManager.Infrastructure.Persistence.Write.Configurations
{
    internal sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            builder.ToTable("students");

            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id)
                .HasConversion(id => id.Value, value => StudentId.From(value))
                .HasColumnName("id");

            // Owned value objects — stored as columns in the same table
            builder.OwnsOne(s => s.Name, n =>
            {
                n.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
                n.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
            });

            builder.OwnsOne(s => s.Email, e =>
            {
                e.Property(x => x.Value).HasColumnName("email").HasMaxLength(200).IsRequired();
                e.HasIndex(x => x.Value).IsUnique();
            });

            builder.Property(s => s.DateOfBirth).HasColumnName("date_of_birth").IsRequired();
            builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");
        }
    }

    internal sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
    {
        public void Configure(EntityTypeBuilder<Subject> builder)
        {
            builder.ToTable("subjects");

            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id)
                .HasConversion(id => id.Value, value => SubjectId.From(value))
                .HasColumnName("id");

            builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            builder.Property(s => s.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
            builder.HasIndex(s => s.Code).IsUnique();

            builder.Property(s => s.Description).HasColumnName("description").HasMaxLength(1000);
            builder.Property(s => s.Credits).HasColumnName("credits").IsRequired();
            builder.Property(s => s.MaxStudents).HasColumnName("max_students").IsRequired();
            builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        }
    }

    internal sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
    {
        public void Configure(EntityTypeBuilder<Enrollment> builder)
        {
            builder.ToTable("enrollments");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasConversion(id => id.Value, value => EnrollmentId.From(value))
                .HasColumnName("id");

            builder.Property(e => e.StudentId)
                .HasConversion(id => id.Value, value => StudentId.From(value))
                .HasColumnName("student_id");

            builder.Property(e => e.SubjectId)
                .HasConversion(id => id.Value, value => SubjectId.From(value))
                .HasColumnName("subject_id");

            builder.Property(e => e.Status)
                .HasConversion<string>()
                .HasColumnName("status")
                .IsRequired();

            builder.Property(e => e.EnrolledAt).HasColumnName("enrolled_at").IsRequired();
            builder.Property(e => e.CompletedAt).HasColumnName("completed_at");

            builder.HasIndex(e => new { e.StudentId, e.SubjectId }).IsUnique();
        }
    }
}
