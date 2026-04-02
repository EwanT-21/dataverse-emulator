using System.Collections.Concurrent;
using Ardalis.Specification;
using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Common;

namespace Dataverse.Emulator.Persistence.InMemory;

public abstract class InMemoryRepository<T> : IRepository<T>
    where T : class, IAggregateRoot
{
    private readonly ConcurrentDictionary<string, T> entities = new(StringComparer.OrdinalIgnoreCase);
    private readonly IInMemorySpecificationEvaluator evaluator = InMemorySpecificationEvaluator.Default;

    public Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var storageKey = GetStorageKey(entity);
        if (!entities.TryAdd(storageKey, entity))
        {
            throw new InvalidOperationException($"Entity '{storageKey}' already exists.");
        }

        return Task.FromResult(entity);
    }

    public async Task<IEnumerable<T>> AddRangeAsync(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default)
    {
        var added = new List<T>();

        foreach (var entity in entities)
        {
            added.Add(await AddAsync(entity, cancellationToken));
        }

        return added;
    }

    public Task<int> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var storageKey = GetStorageKey(entity);
        if (!entities.ContainsKey(storageKey))
        {
            throw new InvalidOperationException($"Entity '{storageKey}' does not exist.");
        }

        entities[storageKey] = entity;
        return Task.FromResult(1);
    }

    public async Task<int> UpdateRangeAsync(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default)
    {
        var updated = 0;

        foreach (var entity in entities)
        {
            updated += await UpdateAsync(entity, cancellationToken);
        }

        return updated;
    }

    public Task<int> DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(entities.TryRemove(GetStorageKey(entity), out _) ? 1 : 0);
    }

    public async Task<int> DeleteRangeAsync(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default)
    {
        var deleted = 0;

        foreach (var entity in entities)
        {
            deleted += await DeleteAsync(entity, cancellationToken);
        }

        return deleted;
    }

    public Task<int> DeleteRangeAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var matchedEntities = Evaluate(specification).ToArray();
        var deleted = 0;

        foreach (var entity in matchedEntities)
        {
            if (entities.TryRemove(GetStorageKey(entity), out _))
            {
                deleted++;
            }
        }

        return Task.FromResult(deleted);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(0);
    }

    public Task<T?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default)
        where TId : notnull
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = Snapshot().FirstOrDefault(candidate => MatchesId(candidate, id));
        return Task.FromResult(entity);
    }

    public Task<T?> FirstOrDefaultAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Evaluate(specification).FirstOrDefault());
    }

    public Task<TResult?> FirstOrDefaultAsync<TResult>(
        ISpecification<T, TResult> specification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Evaluate(specification).FirstOrDefault());
    }

    public Task<T?> SingleOrDefaultAsync(
        ISingleResultSpecification<T> specification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Evaluate((ISpecification<T>)specification).SingleOrDefault());
    }

    public Task<TResult?> SingleOrDefaultAsync<TResult>(
        ISingleResultSpecification<T, TResult> specification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Evaluate((ISpecification<T, TResult>)specification).SingleOrDefault());
    }

    public Task<List<T>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Snapshot().ToList());
    }

    public Task<List<T>> ListAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Evaluate(specification).ToList());
    }

    public Task<List<TResult>> ListAsync<TResult>(
        ISpecification<T, TResult> specification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Evaluate(specification).ToList());
    }

    public Task<int> CountAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Evaluate(specification).Count());
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(entities.Count);
    }

    public Task<bool> AnyAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Evaluate(specification).Any());
    }

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(!entities.IsEmpty);
    }

    public IAsyncEnumerable<T> AsAsyncEnumerable(ISpecification<T> specification)
        => ToAsyncEnumerable(Evaluate(specification));

    protected IReadOnlyCollection<T> Snapshot() => entities.Values.ToArray();

    protected abstract string GetStorageKey(T entity);

    protected abstract bool MatchesId<TId>(T entity, TId id);

    private IEnumerable<T> Evaluate(ISpecification<T> specification)
        => evaluator.Evaluate(Snapshot(), specification);

    private IEnumerable<TResult> Evaluate<TResult>(ISpecification<T, TResult> specification)
        => evaluator.Evaluate(Snapshot(), specification);

    private static async IAsyncEnumerable<T> ToAsyncEnumerable(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }
}
