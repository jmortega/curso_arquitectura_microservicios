using MediatR;

namespace AcademyManager.Domain.Common;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
