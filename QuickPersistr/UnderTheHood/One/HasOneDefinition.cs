using QuickCheckr;
using QuickFuzzr;

namespace QuickPersistr.UnderTheHood.One;

public record HasOneDefinition<TEntity, TReader, TChild, TId>(
    IdentitySelector<TEntity, TId> Identity,
    IPersistenceSpecification<TReader> ChildSpecification,
    IReadOnlyList<Shrinker> EntityShrinkers,
    string RelationshipKey)
where TEntity : class
where TChild : class
{
    public FuzzrOf<TChild> ChildFuzzr => ChildSpecification.GetCreator<TChild>();
    public Action<TEntity, TChild> Set { get; init; } = null!;
    public Action<TEntity>? Clear { get; init; }
    public Action<TEntity, TEntity, TChild>? Reassign { get; init; }
    public Func<IPersistenceReader<TReader>, TId, TEntity> Reload { get; init; } = null!;
    public Func<TEntity, TChild, bool> Contains { get; init; } = null!;
    public Func<TEntity, bool>? Empty { get; init; }
}
