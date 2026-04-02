using Ardalis.Specification;
using Dataverse.Emulator.Domain.Common;

namespace Dataverse.Emulator.Application.Abstractions;

public interface IRepository<T> : IRepositoryBase<T>, IReadRepository<T>
    where T : class, IAggregateRoot
{
}
