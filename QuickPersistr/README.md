# QuickPersistr

Property-based persistence testing for .NET.

QuickPersistr generates entities and sequences of create, read, update, and delete operations, then checks the persisted result through fresh sessions. Failed runs are shrunk to a small reproducible case and include a seed for replay.

> This package contains the engine and persistence contracts. EF Core users normally install one of the provider packages instead. This brings this package in transitively.

## Choose a package

| Package | Use it for |
| --- | --- |
| `QuickPersistr.EntityFrameworkCore.Sqlite` | Isolated in-memory SQLite tests |
| `QuickPersistr.EntityFrameworkCore.PostgreSql` | Isolated databases on a real PostgreSQL server |
| `QuickPersistr.EntityFrameworkCore` | EF Core with externally managed database setup |
| `QuickPersistr` | Implementing an adapter for another persistence technology |

To implement an adapter, provide `IPersistenceScope<TReader>` and `IPersistenceReader<TReader>`. A scope must be able to commit changes and discard its current session without discarding committed data. Composite identities use the array overloads, and optimistic-concurrency specifications require `OpenConcurrentSession()`.

```csharp
public interface IPersistenceReader<TReader>
{
    TResult Query<TResult>(Func<TReader, TResult> query);
}
```

The complete contracts are in [`IPersistenceScope.cs`](https://github.com/kilfour/QuickPersistr/blob/main/QuickPersistr/IPersistenceScope.cs) and [`IPersistenceReader.cs`](https://github.com/kilfour/QuickPersistr/blob/main/QuickPersistr/IPersistenceReader.cs).

## Aggregate roots and dependents

Specifications registered with `.Entities(...)` are independently persisted roots. A child specification supplied through `.From(...)` in a `HasOne` or `HasMany` contract is instead exercised through its parent, so required foreign keys are valid. QuickPersistr runs its full configured contract: generated and unique identities, property reads and updates, domain updates, optimistic concurrency, nested relationships, rejected operations, deletion, and post-delete expectations. Generated child updates leave relationship-owned foreign keys untouched, and rejected creates attach through the parent before commit. Register the child separately only when it can also be validly persisted without its parent.

## Replaying a failure

Every failed run reports its seed. Pass that seed to `Run` to reproduce the same generated scenario:

```csharp
persister.Run(seed);
```

See the [project README](https://github.com/kilfour/QuickPersistr) for the full DSL, examples, and provider guidance.

QuickPersistr is an early preview targeting .NET 8. Until version 1.0, its public API may change between releases.
