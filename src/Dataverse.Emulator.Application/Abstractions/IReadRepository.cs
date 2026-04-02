using Ardalis.Specification;
using Dataverse.Emulator.Domain.Common;

namespace Dataverse.Emulator.Application.Abstractions;

public interface IReadRepository<T> : IReadRepositoryBase<T>
    where T : class, IAggregateRoot
{
}
