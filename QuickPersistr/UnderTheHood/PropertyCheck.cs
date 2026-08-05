namespace QuickPersistr.UnderTheHood;

public record PropertyCheck<TEntity>(
    string Name,
    Func<TEntity, object?> GetValue,
    Func<TEntity, TEntity, bool> Check);
