namespace QuickPersistr.UnderTheHood;

public class PersistrScope(string name)
{
    public PersistrReader<TContext> Scope<TContext>(Func<TContext> contextFactory) => new(name, contextFactory);
}
