namespace QuickPersistr.UnderTheHood;

public record PropertyCheck<TEntity>(string Name, Func<TEntity, object?> GetValue)
{
    public Func<TEntity, TEntity, bool> Check = (a, b) => Equals(GetValue(a), GetValue(b));
}
