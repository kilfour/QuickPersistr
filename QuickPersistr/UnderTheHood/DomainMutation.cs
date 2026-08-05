namespace QuickPersistr.UnderTheHood;

public sealed record DomainMutation<TEntity>(
    string Description,
    Action<TEntity> Apply)
where TEntity : class;
