using QuickCheckr;

namespace QuickPersistr;

public static class Persistr
{
    public static PersistrBuilderScope Named(string name) => new(name);
}

public class PersistrBuilderScope(string name)
{
    public PersisterBuilderEntities Scope(Func<IPersistenceScope> scopeFactory)
        => new(name, scopeFactory);
}


public class PersisterBuilderEntities(
    string name,
    Func<IPersistenceScope> scopeFactory)
{
    public PersisterRunner Entities(params IPersistence[] entities)
        => new(name, scopeFactory, entities);
}


public class PersisterRunner(
    string name,
    Func<IPersistenceScope> scopeFactory,
    IPersistence[] entities)
{
    public void Run()
    {
        GetCheckr()
            .Configure(a => a with { FileAs = name })
            .Run(1.Runs(), (entities.Length * 4).ExecutionsPerRun());
    }

    public CheckrOf<Case> GetCheckr() =>
        from scope in Trackr.Stashed(scopeFactory)
        from _ in Checkr.Sequence([.. entities.SelectMany(a => a.Define().ToCheckrs(scope))])
        select Case.Closed;
}