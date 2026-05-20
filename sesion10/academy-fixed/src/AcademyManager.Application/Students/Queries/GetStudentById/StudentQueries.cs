using AcademyManager.Application.Common.Interfaces;
using AcademyManager.Application.ReadModels;
using MediatR;

namespace AcademyManager.Application.Students.Queries.GetStudentById
{
    // ─── GetById Query ───────────────────────────────────────────────────────────

    public sealed record GetStudentByIdQuery(Guid StudentId) : IRequest<StudentReadModel?>;

    public sealed class GetStudentByIdHandler : IRequestHandler<GetStudentByIdQuery, StudentReadModel?>
    {
        private readonly IStudentReadRepository _readRepository;

        public GetStudentByIdHandler(IStudentReadRepository readRepository) =>
            _readRepository = readRepository;

        public Task<StudentReadModel?> Handle(GetStudentByIdQuery request, CancellationToken ct) =>
            _readRepository.GetByIdAsync(request.StudentId, ct);
    }

    // ─── GetAll Query ─────────────────────────────────────────────────────────────

    namespace AcademyManager.Application.Students.Queries.GetAllStudents
    {
        public sealed record GetAllStudentsQuery(int Page = 1, int PageSize = 20)
            : IRequest<PagedResult<StudentReadModel>>;

        public sealed record PagedResult<T>(IEnumerable<T> Items, long TotalCount, int Page, int PageSize)
        {
            public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        }

        public sealed class GetAllStudentsHandler : IRequestHandler<GetAllStudentsQuery, PagedResult<StudentReadModel>>
        {
            private readonly IStudentReadRepository _readRepository;

            public GetAllStudentsHandler(IStudentReadRepository readRepository) =>
                _readRepository = readRepository;

            public async Task<PagedResult<StudentReadModel>> Handle(
                GetAllStudentsQuery request, CancellationToken ct)
            {
                var students = await _readRepository.GetAllAsync(request.Page, request.PageSize, ct);
                var total = await _readRepository.CountAsync(ct);
                return new PagedResult<StudentReadModel>(students, total, request.Page, request.PageSize);
            }
        }
    }
}
