namespace QuickPersistr.UnderTheHood;

public record PropertyCheck<TEntity>(
    string Name,
    Func<TEntity, object?> GetValue,
    Func<object?, object?, bool> AreEqual)
{
    public bool Check(TEntity expected, TEntity actual) =>
        AreEqual(GetValue(expected), GetValue(actual));

    public bool Check(object? expected, TEntity actual) =>
        AreEqual(expected, GetValue(actual));
}
