using QuickCheckr;
using QuickFuzzr;

namespace QuickPersistr.UnderTheHood.Many;

public record HasManyDefinition<TEntity, TReader, TChild, TId>(
    IdentitySelector<TEntity, TId> Identity,
    FuzzrOf<TChild> ChildFuzzr,
    IReadOnlyList<Shrinker> EntityShrinkers)
where TEntity : class
where TChild : class
{
    public Action<TEntity, TChild> Add { get; init; } = null!;
    public Action<TEntity, TChild> Remove { get; init; } = null!;
    public Action<TEntity, TEntity, TChild>? Reassign { get; init; }
    public Action<TEntity> Clear { get; init; } = null!;
    public Func<IPersistenceReader<TReader>, TId, TEntity> Reload { get; init; } = null!;
    public Func<TEntity, TChild, bool> Contains { get; init; } = null!;
    public Func<TEntity, bool> Empty { get; init; } = null!;
}
