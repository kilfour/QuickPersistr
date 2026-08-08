# <img src='icon.png' width='40' align='top'/> QuickPersistr
> **Look out, honey, 'cause I'm using technology**

Property-based persistence testing for .NET.

[![NuGet](https://img.shields.io/nuget/v/QuickPersistr.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/QuickPersistr)
[![CI](https://img.shields.io/github/actions/workflow/status/kilfour/QuickPersistr/ci.yml?branch=main&style=flat-square&label=build)](https://github.com/kilfour/QuickPersistr/actions/workflows/ci.yml)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/license-MIT-success?style=flat-square)](https://github.com/kilfour/QuickPersistr/blob/main/LICENSE)

**QuickPersistr** exercises a persistence mapping with generated entities and sequences of create, read, update, and delete operations. It tests the boring little details where real bugs hide: value converters, backing fields, ownership, navigation synchronisation, key generation, delete behaviour, stale writes, and change-tracking assumptions.

These are integration tests, not model-metadata checks. QuickPersistr writes an entity, commits it, discards the current session, and reads it back through a fresh one. A mapping only passes when the persisted state behaves the way the domain model says it should.

When a generated scenario fails, the underlying [QuickCheckr](https://github.com/kilfour/QuickCheckr) engine shrinks the sequence and its inputs into a small, reproducible case.

> QuickPersistr tests what your persistence layer *does*, not what its configuration claims it will do.

> [!IMPORTANT]
> QuickPersistr is an early preview. Until version 1.0, the public API and package structure may change between releases.

## Installation

Most EF Core users only need one provider package; it brings in the engine and shared EF adapter transitively.

| Package | Use it for |
| --- | --- |
| `QuickPersistr.EntityFrameworkCore.Sqlite` | Fast, isolated tests backed by in-memory SQLite |
| `QuickPersistr.EntityFrameworkCore.PostgreSql` | Tests against the behavior of a real PostgreSQL server |
| `QuickPersistr.EntityFrameworkCore` | EF Core when database creation and lifetime are managed externally |
| `QuickPersistr` | A custom adapter for another persistence technology |

For the quickest start:

```bash
dotnet add package QuickPersistr.EntityFrameworkCore.Sqlite --version 0.0.1
```

The EF adapters currently target .NET 8 and support EF Core 8.x.

## Quick start

Suppose an EF Core mapping silently ignores changes to `Book.Description`:

```csharp
modelBuilder.Entity<Book>()
    .Property(book => book.Description)
    .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
```

Describe the entity's persistence contract:

```csharp
public class BookPersistence : Persistence<LibraryDbContext, Book>
{
    public override IPersistenceSpecification<LibraryDbContext> Define() =>
        Entity
            .PrimaryKey(book => book.Id)
            .Property(book => book.Title)
            .Property(book => book.Description)
            .Persist();
}
```

Then provide a persistence scope and run it:

```csharp
using QuickPersistr.EntityFrameworkCore.Sqlite;

public class BookPersistenceTests
{
    [Fact]
    public void Mapping_matches_the_contract() =>
        Persistr.Named("Books")
            .Scope(() => new SqlitePersistenceScope<LibraryDbContext>(
                options => new LibraryDbContext(options)))
            .Entities(new BookPersistence())
            .Run();
}
```

QuickPersistr creates varied `Book` instances, exercises the mapping, and reports the persistence contract that was broken:

```text
------------------------------------------------------------
Test:                    Books
Original failing run:    3 executions
Minimal failing case:    2 executions (after 9 shrinks)
------------------------------------------------------------
 Executed: Create Book
  - Entity = Book-1
------------------------------------------------------------
 Executed: Update Book
  - Entity = Book-1
======================================================
 !! Expectation Failed: Can Update Book.Description
      Expected: "dzrqiz"
      Actual:   "h"
======================================================
```

The generated values will vary. Failed runs include a seed, so the same case can be replayed with `.Run(seed)`. See the [SQLite package quick start](https://github.com/kilfour/QuickPersistr/blob/main/QuickPersistr.EntityFrameworkCore.Sqlite/README.md) for a complete model, context, specification, and test.

## What it checks

A basic entity specification covers:

- creation and retrieval through a fresh persistence session;
- generated identities being non-default and unique;
- round-tripping every declared property;
- updates surviving a commit and reload;
- deletion and optional post-delete assertions.

The DSL also supports:

- generic and composite identities;
- custom property equality;
- explicit domain-method mutations;
- one-to-one and one-to-many add, remove, clear, replace, and reassignment scenarios;
- dependent property checks executed through their parent relationship;
- rejected creates, updates, and deletes, including verification that existing state was preserved;
- optimistic-concurrency conflicts through concurrent sessions;
- custom shrinkers and generation configuration.

## Persistence adapter

QuickPersistr is not coupled to EF Core. Provider adapters are separate, opt-in packages; any persistence technology can instead be used by implementing a scope and a reader. In abridged form, the contracts are:

```csharp
public interface IPersistenceScope
{
    TEntity GetById<TEntity>(object? id) where TEntity : class;
    TEntity Add<TEntity>(TEntity entity);
    void DeleteById<TEntity>(object? id) where TEntity : class;
    void Commit();
    void StartNewSession();
}

public interface IPersistenceScope<TReader> : IPersistenceScope
{
    IPersistenceReader<TReader> Reader { get; }
}

public interface IPersistenceReader<TReader>
{
    TResult Query<TResult>(Func<TReader, TResult> query);
}
```

Composite-key tests additionally use the array overloads of `GetById` and `DeleteById`. Optimistic-concurrency tests require `OpenConcurrentSession()` and an `IConcurrentPersistenceScope<TReader>` implementation. See the [scope interfaces](https://github.com/kilfour/QuickPersistr/blob/main/QuickPersistr/IPersistenceScope.cs) and [provider-neutral EF Core adapter](https://github.com/kilfour/QuickPersistr/blob/main/QuickPersistr.EntityFrameworkCore/EfPersistenceScope.cs) for the complete contracts and an implementation.

`StartNewSession()` is important: it must discard identity maps and change tracking without discarding committed data. Otherwise a test may only prove that the in-memory session still knows about an entity.

### EF Core providers

`QuickPersistr.EntityFrameworkCore.Sqlite` contains `SqlitePersistenceScope<TDbContext>`, the zero-configuration adapter shown above. Each scope owns an isolated in-memory SQLite database, and supports fresh and concurrent EF contexts over that database.

`QuickPersistr.EntityFrameworkCore.PostgreSql` provides the same isolation against a real PostgreSQL server:

```csharp
using QuickPersistr.EntityFrameworkCore.PostgreSql;

Persistr.Named("Books on PostgreSQL")
    .Scope(() => new PostgreSqlPersistenceScope<LibraryDbContext>(
        Environment.GetEnvironmentVariable("QUICKPERSISTR_POSTGRES")!,
        options => new LibraryDbContext(options)))
    .Entities(new BookPersistence())
    .Run();
```

The connection string identifies the server and maintenance database. Its user must be allowed to create and drop databases. A uniquely named database is created for each scope and removed when the scope is disposed. If database provisioning is handled elsewhere, use the provider-neutral `QuickPersistr.EntityFrameworkCore.EfPersistenceScope<TDbContext>` with a `Func<TDbContext>` instead.

The current adapter packages support EF Core 8. Their EF/provider dependency ranges are bounded below EF Core 9, so choosing an adapter does not add EF to the main `QuickPersistr` package.

SQLite is useful for fast feedback, but it cannot prove provider-specific SQL, type mappings, conversions, constraints, or concurrency behavior. Use the adapter for your production provider when those details belong to the persistence contract.

## Defining richer contracts

The fluent specification grows with the persistence behaviour you care about:

```csharp
Entity
    .PrimaryKey(course => course.Id)
    .Property(course => course.Title)
    .Update(course => course.Publish())
    .AfterDelete(
        "Removes associated enrolments",
        (reader, course) => reader.Query(db =>
            !db.Enrolments.Any(x => x.CourseId == course.Id)))
    .HasMany(many => many
        .From(new StudentPersistence())
        .Add((course, student) => course.Students.Add(student))
        .Remove((course, student) => course.Students.Remove(student))
        .Clear(course => course.Students.Clear())
        .Reload((reader, id) => reader.Query(db => db.Courses
            .Include(course => course.Students)
            .Single(course => course.Id == id)))
        .Contains((course, student) => course.Students.Any(
            candidate => candidate.Id == student.Id))
        .Empty(course => course.Students.Count == 0))
    .Persist();
```

Only declare properties and behaviours that belong to the persistence contract. Navigation properties that are configured separately can be excluded from automatic generation with `DomainConfiguration(...)` and then exercised explicitly through `HasOne` or `HasMany`.

### Aggregate roots and dependent entities

Specifications passed to `.Entities(...)` are treated as independently persistable roots. QuickPersistr runs their standalone create, read, update, and delete scenarios.

A specification passed to `.From(...)` inside `HasOne` or `HasMany` is a full dependent contract. QuickPersistr creates the child through the parent relationship, commits, opens fresh sessions, and runs the same configured behaviors as it does for a root: generated and unique identities, property reads and updates, explicit domain updates, optimistic concurrency, nested relationships, rejected operations, deletion, and `AfterDelete(...)` expectations. Rejected creates are attached through the parent before committing, so required foreign keys remain valid while the intended rejection is tested:

```csharp
Persistr.Named("Courses")
    .Scope(() => new CourseScope())
    .Entities(new CoursePersistence())
    .Run();
```

```csharp
Entity
    .PrimaryKey(course => course.Id)
    .HasMany(many => many
        .From(new StudentPersistence())
        .Add((course, student) => course.Students.Add(student))
        .Reload((reader, id) => reader.Query(db => db.Courses
            .Include(course => course.Students)
            .Single(course => course.Id == id)))
        .Contains((course, student) => course.Students.Any(
            candidate => candidate.Id == student.Id)))
    .Persist();
```

Here `StudentPersistence` is executed completely after the course persists its students. Generated updates mutate only properties declared by the child specification, leaving relationship-owned foreign keys alone; explicit child mutations still do exactly what their specification declares. Child relationships are evaluated recursively. The destructive child-delete phase runs only after the enclosing parent relationship expectations succeed, keeping failure shrinking reproducible.

Do not also pass the child to `.Entities(...)` unless it can validly be created without its parent. Parent removal, clearing, replacement, and reassignment remain relationship behaviors, while the child's configured delete and post-delete contract is checked independently through the persisted dependent.

## Development

Build and run the executable examples:

```bash
dotnet build QuickPersistr.sln
dotnet test QuickPersistr.sln
```

To verify the distributable artifacts rather than project references, run the package smoke test. It packs all four packages into a clean local feed, restores a standalone consumer, executes SQLite, and compile-checks PostgreSQL:

```powershell
./Scripts/test-packages.ps1
```

To test and create the release packages under `artifacts/packages/<version>`:

```powershell
./Scripts/package.ps1
```

Add `-Publish` to push the packages to NuGet. The script reads `NUGET_API_KEY` from the process environment, falling back to the ignored repository `.env` file, and uses `--skip-duplicate` when publishing. Use `-SkipTests` only when the test suite has already passed.

## Dependencies

- QuickCheckr: stateful property-based testing, behavioural shrinking, and failure reports.
- [QuickFuzzr](https://github.com/kilfour/QuickFuzzr): generated entities, values, and domain configuration.

EF Core and SQLite are used by the test suite through optional adapter projects; they are not required by the QuickPersistr library itself. PostgreSQL support is supplied through the optional Npgsql-based adapter and requires an external PostgreSQL server at runtime.

## License

This project is licensed under the [MIT License](https://github.com/kilfour/QuickPersistr/blob/main/LICENSE).
